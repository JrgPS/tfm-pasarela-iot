using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Iot.Protocol;

namespace Iot.Simulator;

// Explicitly coordinated fault test. No automatic retries hide the first outcome.
internal static class FaultProbe
{
    public static async Task Run(string[] args)
    {
        string Option(string name, string fallback) => Array.IndexOf(args, "--" + name) is var i && i >= 0 ? args[i + 1] : fallback;
        var scenario = Option("fault-probe", "database");
        if (scenario is not ("database" or "overload")) throw new ArgumentException("Unknown fault scenario.");
        var prefix = Option("coordination", "results/fault-probe");
        var secrets = Environment.GetEnvironmentVariable("IOT_SECRETS")!;
        var devices = JsonSerializer.Deserialize<List<SimulatorDevice>>(await File.ReadAllTextAsync(Path.Combine(secrets, "devices.json")))!;
        var pin = SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(secrets, "server.cer")));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(120));
        var token = deadline.Token;
        var links = new List<ProbeLink>();
        try
        {
            foreach (var device in devices.Take(scenario == "database" ? 1 : 32))
            {
                var link = new ProbeLink(device, pin);
                links.Add(link);
                await link.Connect(token);
                link.Before = (await link.Send(link.Frame("003", "-"), token)).Payload;
                link.Operation = link.Frame("002", "ADD,+00000000001,+00000000002");
            }
            await File.WriteAllTextAsync(prefix + ".ready", "ready", token);
            await Wait(".go");
            var first = await Task.WhenAll(links.Select(async link =>
            {
                try { return (await link.Send(link.Operation, token)).Code; }
                catch (Exception ex) when (ex is IOException or System.Net.Sockets.SocketException or OperationCanceledException)
                { return "transport"; }
            }));
            await File.WriteAllTextAsync(prefix + ".observed", JsonSerializer.Serialize(first), token);
            if (scenario == "database" && first.Any(c => c == "000")) throw new InvalidOperationException("Accepted during database outage.");
            if (scenario == "overload" && !first.Contains("013")) throw new InvalidOperationException("Capacity rejection was not observed.");
            await Wait(".recover");
            // Reconnect, resend the IDENTICAL operation twice, then compare totals.
            foreach (var link in links)
            {
                await link.Connect(token);
                var once = await link.Send(link.Operation, token);
                var twice = await link.Send(link.Operation, token);
                if (once.Code != "000" || twice.Code != "000" || once.Payload != "SUM,+3" || twice.Payload != once.Payload)
                    throw new InvalidOperationException("Recovery/replay failed.");
                var after = (await link.Send(link.Frame("003", "-"), token)).Payload.Split(',');
                var before = link.Before.Split(',');
                if (long.Parse(after[1]) != long.Parse(before[1]) + 1 || decimal.Parse(after[2]) != decimal.Parse(before[2]) + 3)
                    throw new InvalidOperationException("Recovery duplicated or lost an operation.");
            }
            var report = new { utc = DateTimeOffset.UtcNow, scenario, devices = links.Count,
                firstOutcomes = first.GroupBy(x => x).ToDictionary(g => g.Key, g => g.Count()),
                recoveredOperations = links.Count, replayedOperations = links.Count, totalsVerified = true };
            await File.WriteAllTextAsync(prefix + ".json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }), token);
            Console.WriteLine(JsonSerializer.Serialize(report));
        }
        finally { foreach (var link in links) link.Dispose(); }

        async Task Wait(string suffix)
        {
            while (!File.Exists(prefix + suffix)) await Task.Delay(100, token);
        }
    }

    private sealed class ProbeLink(SimulatorDevice device, byte[] pin) : IDisposable
    {
        private readonly byte[] key = Convert.FromBase64String(device.Key);
        private TcpClient? tcp;
        private SslStream? stream;
        private FrameReader? reader;
        public string Before { get; set; } = "";
        public string Operation { get; set; } = "";
        public string Frame(string action, string payload) => WireProtocol.CreateRequest(device.Identity, action, payload, key);
        public async Task Connect(CancellationToken token)
        {
            stream?.Dispose(); tcp?.Dispose();
            tcp = new TcpClient { NoDelay = true };
            await tcp.ConnectAsync("proxy", 7443, token);
            stream = new SslStream(tcp.GetStream(), false, (_, cert, _, errors) => cert is not null &&
                (errors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0 &&
                CryptographicOperations.FixedTimeEquals(pin, SHA256.HashData(cert.GetRawCertData())));
            await stream.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "proxy" }, token);
            reader = new FrameReader(stream, TimeSpan.FromSeconds(15));
            if ((await Send(Frame("001", "-"), token)).Code != "000") throw new IOException("Initialization failed.");
        }
        public async Task<Response> Send(string frame, CancellationToken token)
        {
            await stream!.WriteAsync(Encoding.ASCII.GetBytes(frame + "\n"), token);
            var result = WireProtocol.ParseResponse(await reader!.ReadAsync(token) ?? throw new IOException("Peer closed."));
            var request = WireProtocol.ParseRequest(frame);
            if (result.Id != request.Id || result.Device != device.Identity || result.Action != request.Action)
                throw new InvalidOperationException("Correlation mismatch.");
            if (result.Code is "009" or "013" && result.Mac == "-")
            {
                if (result.Merchant != "000000000") throw new InvalidOperationException("Unsigned merchant disclosure.");
                return result;
            }
            if (!WireProtocol.Verify(result.Canonical, result.Mac, key) || result.Merchant != device.Merchant)
                throw new InvalidOperationException("Authentication failed.");
            return result;
        }
        public void Dispose() { stream?.Dispose(); tcp?.Dispose(); CryptographicOperations.ZeroMemory(key); }
    }
}
