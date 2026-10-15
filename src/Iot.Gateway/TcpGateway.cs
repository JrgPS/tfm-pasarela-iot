using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Iot.Protocol;

namespace Iot.Gateway;

public sealed class TcpGateway(IHttpClientFactory clients, ILogger<TcpGateway> logger) : BackgroundService
{
    private readonly ConcurrentDictionary<string, Task> connections = new();
    private readonly string replica = Environment.GetEnvironmentVariable("HOSTNAME") ?? Environment.MachineName;
    private TcpListener? listener;
    public bool Ready { get; private set; }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var secrets = Environment.GetEnvironmentVariable("IOT_SECRETS") ?? Path.Combine(Environment.CurrentDirectory, "secrets");
        using var cert = X509CertificateLoader.LoadPkcs12FromFile(Path.Combine(secrets, "server.pfx"),
            File.ReadAllText(Path.Combine(secrets, "cert-password")).Trim());
        var port = int.Parse(Environment.GetEnvironmentVariable("TCP_PORT") ?? "7443");
        var maxConnections = int.Parse(Environment.GetEnvironmentVariable("MAX_CONNECTIONS") ?? "2000");
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start(); Ready = true;
        logger.LogInformation("TCP TLS gateway listening on {Port}, replica {Replica}", port, replica);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var socket = await listener.AcceptTcpClientAsync(stoppingToken);
                foreach (var pair in connections.Where(p => p.Value.IsCompleted)) connections.TryRemove(pair.Key, out _);
                if (connections.Count >= maxConnections) { socket.Dispose(); continue; }
                var id = Guid.NewGuid().ToString();
                connections[id] = ServeAsync(socket, id, cert, stoppingToken);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        finally
        {
            Ready = false; listener.Stop();
            try { await Task.WhenAll(connections.Values).WaitAsync(TimeSpan.FromSeconds(10)); }
            catch (TimeoutException) { logger.LogWarning("Shutdown timed out waiting for clients"); }
        }
    }

    private async Task ServeAsync(TcpClient socket, string id, X509Certificate2 certificate, CancellationToken stoppingToken)
    {
        var opened = false;
        var reason = "peer_closed";
        using (socket)
        using (var tls = new SslStream(socket.GetStream(), false))
        using (var client = clients.CreateClient("registry"))
        {
            try
            {
                socket.NoDelay = true;
                using (var handshake = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken))
                {
                    handshake.CancelAfter(TimeSpan.FromSeconds(10));
                    await tls.AuthenticateAsServerAsync(new SslServerAuthenticationOptions
                    {
                        ServerCertificate = certificate,
                        EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
                        ClientCertificateRequired = false
                    }, handshake.Token);
                }
                using (var response = await client.PostAsJsonAsync("/internal/connections/open",
                           new ConnectionEvent(id, replica), stoppingToken)) response.EnsureSuccessStatusCode();
                opened = true;
                var frames = new FrameReader(tls);
                var initialized = false;
                while (!stoppingToken.IsCancellationRequested)
                {
                    using var deadline = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    deadline.CancelAfter(TimeSpan.FromSeconds(initialized ? 120 : 15));
                    var frame = await frames.ReadAsync(deadline.Token);
                    if (frame is null) break;
                    ProcessResult result;
                    try
                    {
                        using var response = await client.PostAsJsonAsync("/internal/messages/process",
                            new ProcessMessage(id, replica, Guid.NewGuid().ToString(), frame), stoppingToken);
                        response.EnsureSuccessStatusCode();
                        result = await response.Content.ReadFromJsonAsync<ProcessResult>(stoppingToken) ??
                                 throw new IOException("Empty registry response.");
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    {
                        logger.LogWarning("Registry unavailable for connection {Connection}, outcome may be uncertain", id);
                        result = new(WireProtocol.Reject(frame, "009"), true);
                    }
                    if (result.Frame is not null)
                    {
                        var parsed = WireProtocol.ParseResponse(result.Frame);
                        if (parsed.Action == "001" && parsed.Code == "000") initialized = true;
                        await tls.WriteAsync(Encoding.ASCII.GetBytes(result.Frame + "\n"), stoppingToken);
                    }
                    if (result.Close) { reason = "application_rejected"; break; }
                }
            }
            catch (ProtocolException ex)
            {
                reason = "protocol_" + ex.Code;
                logger.LogWarning("Protocol rejection {Code}, connection {Connection}", ex.Code, id);
                if (opened)
                {
                    try
                    {
                        using var response = await client.PostAsJsonAsync("/internal/audit/rejections",
                            new AuditRejection(id, replica, Guid.NewGuid().ToString(), ex.Code, ex.Message));
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception auditError) when (auditError is HttpRequestException or TaskCanceledException)
                    { logger.LogWarning("Audit incomplete for connection {Connection}", id); }
                }
            }
            catch (Exception ex) when (ex is IOException or SocketException or AuthenticationException or
                                       OperationCanceledException or HttpRequestException)
            {
                reason = stoppingToken.IsCancellationRequested ? "shutdown" : "transport_or_dependency_error";
                logger.LogInformation("Connection {Connection} ended: {Reason}", id, reason);
            }
            finally
            {
                if (opened)
                {
                    try
                    {
                        using var response = await client.PostAsJsonAsync("/internal/connections/close",
                            new ConnectionEvent(id, replica, reason));
                        response.EnsureSuccessStatusCode();
                    }
                    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
                    { logger.LogWarning("Audit incomplete on close for {Connection}", id); }
                }
            }
        }
    }
}
