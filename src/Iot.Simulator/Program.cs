using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using Iot.Protocol;
using Iot.Simulator;

string Option(string name, string fallback)
{
    var index = Array.IndexOf(args, "--" + name);
    return index < 0 ? fallback : index + 1 < args.Length ? args[index + 1] : throw new ArgumentException(name);
}
var secrets = Environment.GetEnvironmentVariable("IOT_SECRETS") ?? Path.Combine(Environment.CurrentDirectory, "secrets");
var host = Option("host", "localhost");
var port = int.Parse(Option("port", "7443"));
var count = int.Parse(Option("devices", "1"));
var operations = int.Parse(Option("operations", "5"));
var interval = int.Parse(Option("interval-ms", "0"));
var ramp = int.Parse(Option("ramp-ms", "10"));
if (count < 1 || operations < 0 || interval < 0 || ramp < 0) throw new ArgumentException("Invalid load parameters.");
var manifest = JsonSerializer.Deserialize<List<SimulatorDevice>>(await File.ReadAllTextAsync(Path.Combine(secrets, "devices.json")))!;
if (count > manifest.Count) throw new ArgumentException("Provision more devices before running this profile.");
var certHash = SHA256.HashData(await File.ReadAllBytesAsync(Path.Combine(secrets, "server.cer")));
using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
var latencies = new ConcurrentBag<double>();
var failures = new ConcurrentBag<string>();
long confirmed = 0, reconnects = 0, retries = 0;
var timer = Stopwatch.StartNew();
var tasks = new List<Task>();
foreach (var device in manifest.Take(count))
{
    tasks.Add(RunDevice(device));
    await Task.Delay(ramp, stop.Token);
}
await Task.WhenAll(tasks);
timer.Stop();
var sorted = latencies.Order().ToArray();
double Percentile(double q) => sorted.Length == 0 ? 0 : sorted[(int)Math.Ceiling(q * sorted.Length) - 1];
var report = new
{
    utc = DateTimeOffset.UtcNow, devices = count, operationsPerDevice = operations,
    confirmedOperations = confirmed, failedDevices = failures.Count,
    reconnections = reconnects, retries, durationSeconds = timer.Elapsed.TotalSeconds,
    uniqueConfirmedPerSecond = confirmed / Math.Max(timer.Elapsed.TotalSeconds, 0.001),
    operationLatencyMs = new { p50 = Percentile(.5), p95 = Percentile(.95), p99 = Percentile(.99) },
    errors = failures.ToArray()
};
Directory.CreateDirectory("results");
var reportPath = Option("output", Path.Combine("results", "simulation-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss") + ".json"));
await File.WriteAllTextAsync(reportPath, JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
Environment.ExitCode = failures.IsEmpty ? 0 : 1;

async Task RunDevice(SimulatorDevice device)
{
    await using var client = new DeviceClient(host, port, certHash, device);
    try
    {
        var before = Totals(await client.SendAsync("003", "-", stop.Token));
        for (var i = 0; i < operations; i++)
        {
            var watch = Stopwatch.StartNew();
            var response = await client.SendAsync("002", "ADD,+00000001234,-00000000567", stop.Token);
            if (response.Code != "000" || response.Payload != "SUM,+667") throw new InvalidOperationException("Wrong sum result.");
            latencies.Add(watch.Elapsed.TotalMilliseconds);
            Interlocked.Increment(ref confirmed);
            if (interval > 0) await Task.Delay(interval, stop.Token);
        }
        var after = Totals(await client.SendAsync("003", "-", stop.Token));
        if (after.Count != before.Count + operations || after.Sum != before.Sum + operations * 667m)
            throw new InvalidOperationException("Persisted totals mismatch.");
    }
    catch (Exception ex) when (ex is not OutOfMemoryException)
    { failures.Add($"{device.Identity.Key}: {ex.GetType().Name}: {ex.Message}"); }
    finally
    {
        Interlocked.Add(ref reconnects, client.Reconnections);
        Interlocked.Add(ref retries, client.Retries);
    }
}

static (long Count, decimal Sum) Totals(Response response)
{
    var fields = response.Payload.Split(',');
    if (response.Code != "000" || fields.Length != 3 || fields[0] != "TOTALS")
        throw new InvalidOperationException("Invalid totals response.");
    return (long.Parse(fields[1], CultureInfo.InvariantCulture), decimal.Parse(fields[2], CultureInfo.InvariantCulture));
}
