using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Iot.Persistence;
using Iot.Protocol;
using Npgsql;
using Xunit;

namespace Iot.Tests;

public sealed class NetworkFactAttribute : FactAttribute
{
    public NetworkFactAttribute()
    {
        if (Environment.GetEnvironmentVariable("IOT_NETWORK_TESTS") != "true")
            Skip = "Opt in with IOT_NETWORK_TESTS=true against the running local Docker demo.";
    }
}

public class NetworkTests
{
    [NetworkFact]
    public async Task RealTlsSupportsFragmentedAndCoalescedFramesInOrder()
    {
        await using var device = await LiveDevice.Create();
        await using var link = await device.Connect();
        var init = device.Frame("001", "-");
        foreach (var b in Encoding.ASCII.GetBytes(init + "\n")) await link.Stream.WriteAsync(new byte[] { b });
        Assert.Equal("READY", (await link.Read()).Payload);
        await link.Stream.WriteAsync(Encoding.ASCII.GetBytes(
            device.Frame("002", "ADD,+00000001234,-00000000567") + "\n" + device.Frame("003", "-") + "\n"));
        Assert.Equal("SUM,+667", (await link.Read()).Payload);
        Assert.Equal("TOTALS,1,+667", (await link.Read()).Payload);
    }

    [NetworkFact]
    public async Task LostApplicationResponseCanBeRetriedOnANewConnectionWithoutDuplicateEffect()
    {
        await using var device = await LiveDevice.Create();
        var request = device.Frame("002", "ADD,+00000000001,+00000000002");
        await using (var first = await device.Connect())
        {
            await first.Send(device.Frame("001", "-"));
            await first.Stream.WriteAsync(Encoding.ASCII.GetBytes(request + "\n"));
            // Wait for database commit but deliberately never consume the TCP application response.
            var committed = false;
            for (var i = 0; i < 100; i++)
            {
                await using var cmd = device.Db.CreateCommand("SELECT count(*) FROM operations WHERE device_id=@id");
                cmd.Parameters.AddWithValue("id", device.Identity.Key);
                if ((long)(await cmd.ExecuteScalarAsync())! == 1) { committed = true; break; }
                await Task.Delay(20);
            }
            Assert.True(committed, "Operation did not commit before the deliberate response loss.");
        }
        await using var second = await device.Connect();
        await second.Send(device.Frame("001", "-"));
        Assert.Equal("SUM,+3", (await second.Send(request)).Payload);
        Assert.Equal("TOTALS,1,+3", (await second.Send(device.Frame("003", "-"))).Payload);
    }

    [NetworkFact]
    public async Task InvalidMacIsRejectedOverTlsWithoutDisclosingMerchant()
    {
        await using var device = await LiveDevice.Create();
        await using var link = await device.Connect();
        var frame = WireProtocol.CreateRequest(device.Identity, "001", "-", new byte[32]);
        var result = await link.Send(frame);
        Assert.Equal("005", result.Code); Assert.Equal("000000000", result.Merchant); Assert.Equal("-", result.Mac);
    }

    [NetworkFact]
    public async Task UnknownIdentityIsRejectedOverTls()
    {
        await using var device = await LiveDevice.Create();
        await using var link = await device.Connect();
        var result = await link.Send(WireProtocol.CreateRequest(new("96", "99", device.Identity.Serial), "001", "-", device.Key));
        Assert.Equal("002", result.Code); Assert.Equal("000000000", result.Merchant);
    }

    [NetworkFact]
    public async Task OversizedFrameIsClosedAndAudited()
    {
        await using var device = await LiveDevice.Create();
        await using var link = await device.Connect();
        await link.Send(device.Frame("001", "-"));
        await link.Stream.WriteAsync(Encoding.ASCII.GetBytes(new string('A', 4096) + "\n"));
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { Assert.Null(await link.Reader.ReadAsync(deadline.Token)); }
        catch (IOException) { /* A TLS transport close is also valid for this unparseable input. */ }
        await using var cmd = device.Db.CreateCommand("""
            SELECT count(*) FROM message_attempts a JOIN connections c ON c.id=a.connection_id
            WHERE c.device_id=@id AND a.code='003'
            """);
        cmd.Parameters.AddWithValue("id", device.Identity.Key);
        Assert.Equal(1L, await cmd.ExecuteScalarAsync());
    }

    private sealed class LiveDevice : IAsyncDisposable
    {
        public required NpgsqlDataSource Db { get; init; }
        public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
        public DeviceId Identity { get; } = new("97", "01", RandomNumberGenerator.GetInt32(1, int.MaxValue).ToString("D11"));

        public static async Task<LiveDevice> Create()
        {
            // These opt-in tests target only the fictitious Docker demo, never a work environment.
            var builder = new NpgsqlConnectionStringBuilder(Environment.GetEnvironmentVariable("IOT_TEST_CONNECTION")!)
                { Database = "iot", MaxPoolSize = 5 };
            if (builder.Host != "db") throw new InvalidOperationException("Network tests require the isolated Docker demo network.");
            var device = new LiveDevice { Db = NpgsqlDataSource.Create(builder.ConnectionString) };
            var protector = new DeviceKeyProtector(Convert.FromBase64String(Settings.Secret("master-key")));
            await using var cmd = device.Db.CreateCommand("""
                INSERT INTO merchants(id,name) VALUES('888888888','Network tests') ON CONFLICT DO NOTHING;
                INSERT INTO devices(id,merchant_id,encrypted_key) VALUES(@id,'888888888',@key);
                INSERT INTO device_totals(device_id) VALUES(@id)
                """);
            cmd.Parameters.AddWithValue("id", device.Identity.Key);
            cmd.Parameters.AddWithValue("key", protector.Encrypt(device.Key));
            await cmd.ExecuteNonQueryAsync();
            return device;
        }

        public string Frame(string action, string payload) => WireProtocol.CreateRequest(Identity, action, payload, Key);
        public async Task<Link> Connect()
        {
            var tcp = new TcpClient { NoDelay = true };
            await tcp.ConnectAsync("proxy", 7443);
            var pin = SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(Settings.SecretDirectory, "server.cer")));
            var tls = new SslStream(tcp.GetStream(), false, (_, cert, _, errors) => cert is not null &&
                (errors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0 &&
                CryptographicOperations.FixedTimeEquals(pin, SHA256.HashData(cert.GetRawCertData())));
            using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = "proxy" }, deadline.Token);
            return new(tcp, tls, Key, Identity);
        }
        public ValueTask DisposeAsync() => Db.DisposeAsync();
    }

    private sealed class Link(TcpClient tcp, SslStream stream, byte[] key, DeviceId identity) : IAsyncDisposable
    {
        public SslStream Stream => stream;
        public FrameReader Reader { get; } = new(stream, TimeSpan.FromSeconds(10));
        public async Task<Response> Read()
        {
            var result = WireProtocol.ParseResponse(await Reader.ReadAsync() ?? throw new IOException("Peer closed."));
            if (result.Code == "000")
            {
                Assert.Equal(identity, result.Device);
                Assert.Equal("888888888", result.Merchant);
                Assert.True(WireProtocol.Verify(result.Canonical, result.Mac, key));
            }
            return result;
        }
        public async Task<Response> Send(string frame)
        {
            await stream.WriteAsync(Encoding.ASCII.GetBytes(frame + "\n"));
            var result = await Read();
            Assert.Equal(WireProtocol.ParseRequest(frame).Id, result.Id);
            return result;
        }
        public ValueTask DisposeAsync()
        { stream.Dispose(); tcp.Dispose(); return ValueTask.CompletedTask; }
    }
}
