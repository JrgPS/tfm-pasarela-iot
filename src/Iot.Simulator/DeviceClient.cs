using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Iot.Protocol;

namespace Iot.Simulator;

public sealed class DeviceClient(string host, int port, byte[] expectedCertHash, SimulatorDevice device) : IAsyncDisposable
{
    private readonly byte[] key = Convert.FromBase64String(device.Key);
    private TcpClient? socket;
    private SslStream? tls;
    private FrameReader? reader;
    public int Reconnections { get; private set; }
    public int Retries { get; private set; }

    public async Task<Response> SendAsync(string action, string payload, CancellationToken token)
    {
        var frame = WireProtocol.CreateRequest(device.Identity, action, payload, key);
        for (var attempt = 0; ; attempt++)
        {
            try
            {
                if (tls is null) await ConnectAsync(token);
                var result = await ExchangeAsync(frame, token);
                if (result.Code == "009") throw new IOException("Registry unavailable, retry original request.");
                return result;
            }
            catch (Exception ex) when (attempt < 3 && !token.IsCancellationRequested &&
                                       ex is IOException or SocketException or OperationCanceledException)
            {
                Retries++;
                await DisconnectAsync();
                await Task.Delay(TimeSpan.FromMilliseconds(200 * (1 << attempt) + Random.Shared.Next(200)), token);
                Reconnections++;
            }
        }
    }

    private async Task ConnectAsync(CancellationToken token)
    {
        socket = new TcpClient { NoDelay = true };
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await socket.ConnectAsync(host, port, timeout.Token);
        tls = new SslStream(socket.GetStream(), false, (_, certificate, _, errors) =>
            certificate is not null && (errors & SslPolicyErrors.RemoteCertificateNameMismatch) == 0 &&
            CryptographicOperations.FixedTimeEquals(SHA256.HashData(certificate.GetRawCertData()), expectedCertHash));
        await tls.AuthenticateAsClientAsync(new SslClientAuthenticationOptions { TargetHost = host }, timeout.Token);
        reader = new FrameReader(tls, TimeSpan.FromSeconds(10));
        var init = await ExchangeAsync(WireProtocol.CreateRequest(device.Identity, "001", "-", key), token);
        if (init.Code == "009") throw new IOException("Initialization dependency unavailable.");
        if (init.Code != "000") throw new InvalidOperationException($"Initialization denied: {init.Code}");
    }

    private async Task<Response> ExchangeAsync(string frame, CancellationToken token)
    {
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(10));
        await tls!.WriteAsync(Encoding.ASCII.GetBytes(frame + "\n"), timeout.Token);
        var incoming = await reader!.ReadAsync(timeout.Token) ?? throw new IOException("Peer closed connection.");
        var response = WireProtocol.ParseResponse(incoming);
        var request = WireProtocol.ParseRequest(frame);
        if (response.Id != request.Id || response.Device != device.Identity || response.Action != request.Action)
            throw new InvalidOperationException("Response correlation mismatch.");
        if (response.Code == "009" && response.Mac == "-") return response;
        if (!WireProtocol.Verify(response.Canonical, response.Mac, key))
            throw new InvalidOperationException("Response authentication failed.");
        if (response.Merchant != device.Merchant) throw new InvalidOperationException("Merchant mismatch.");
        return response;
    }

    private ValueTask DisconnectAsync()
    {
        tls?.Dispose(); socket?.Dispose(); tls = null; reader = null; socket = null;
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
        CryptographicOperations.ZeroMemory(key);
    }
}
