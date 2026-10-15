using System.Security.Cryptography;
using System.Text;
using Iot.Persistence;
using Iot.Protocol;
using Iot.Registry;
using Npgsql;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 16 * 1024);
builder.Services.AddSingleton(Settings.DataSource());
builder.Services.AddSingleton(new DeviceKeyProtector(Convert.FromBase64String(Settings.Secret("master-key"))));
builder.Services.AddSingleton<RegistryStore>();
var app = builder.Build();
var apiKey = Encoding.UTF8.GetBytes(Settings.Secret("api-key"));
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/internal"))
    {
        var supplied = Encoding.UTF8.GetBytes(context.Request.Headers["X-Iot-Key"].ToString());
        if (!CryptographicOperations.FixedTimeEquals(apiKey, supplied))
        { context.Response.StatusCode = 401; return; }
    }
    await next(context);
});
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (NpgsqlDataSource db, CancellationToken token) =>
{
    try
    {
        await using var cmd = db.CreateCommand("SELECT 1 FROM devices LIMIT 1");
        await cmd.ExecuteScalarAsync(token);
        return Results.Ok(new { status = "ready" });
    }
    catch (Exception ex) when (ex is NpgsqlException or TimeoutException)
    { return Results.StatusCode(503); }
});
app.MapPost("/internal/connections/open", async (ConnectionEvent value, RegistryStore store, CancellationToken t) =>
{ await store.OpenAsync(value, t); return Results.NoContent(); });
app.MapPost("/internal/connections/activity", async (ConnectionEvent value, RegistryStore store, CancellationToken t) =>
{ await store.ActivityAsync(value, t); return Results.NoContent(); });
app.MapPost("/internal/connections/close", async (ConnectionEvent value, RegistryStore store, CancellationToken t) =>
{ await store.CloseAsync(value, t); return Results.NoContent(); });
app.MapPost("/internal/messages/process", async (ProcessMessage value, RegistryStore store, CancellationToken t) =>
{
    if (value.Frame.Length >= WireProtocol.MaxFrameBytes || value.Gateway.Length > 128 ||
        !Guid.TryParse(value.ConnectionId, out _) || !Guid.TryParse(value.EventId, out _)) return Results.BadRequest();
    return Results.Ok(await store.ProcessAsync(value, t));
});
app.MapPost("/internal/audit/rejections", async (AuditRejection value, RegistryStore store, CancellationToken t) =>
{ await store.RejectAsync(value, t); return Results.NoContent(); });
app.Run();
