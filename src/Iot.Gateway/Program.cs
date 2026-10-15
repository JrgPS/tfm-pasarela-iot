using Iot.Gateway;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHttpClient("registry", client =>
{
    client.BaseAddress = new Uri(Environment.GetEnvironmentVariable("REGISTRY_URL") ?? "http://localhost:8080");
    var secrets = Environment.GetEnvironmentVariable("IOT_SECRETS") ?? Path.Combine(Environment.CurrentDirectory, "secrets");
    client.DefaultRequestHeaders.Add("X-Iot-Key", File.ReadAllText(Path.Combine(secrets, "api-key")).Trim());
    client.Timeout = TimeSpan.FromSeconds(5);
});
builder.Services.AddSingleton<TcpGateway>();
builder.Services.AddHostedService(services => services.GetRequiredService<TcpGateway>());
var app = builder.Build();
app.MapGet("/health/live", () => Results.Ok());
app.MapGet("/health/ready", (TcpGateway gateway) => gateway.Ready ? Results.Ok() : Results.StatusCode(503));
app.Run();
