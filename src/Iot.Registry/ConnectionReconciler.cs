using Npgsql;

namespace Iot.Registry;

public sealed class ConnectionReconciler(RegistryStore store, ILogger<ConnectionReconciler> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30));
        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    var count = await store.ReconcileLostAsync(TimeSpan.FromMinutes(3), stoppingToken);
                    if (count > 0) logger.LogInformation("Reconciled {Count} stale connections as inferred losses", count);
                }
                catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
                { logger.LogWarning("Connection reconciliation postponed: database unavailable"); }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
    }
}
