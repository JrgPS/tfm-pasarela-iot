using System.Net;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using Iot.Persistence;
using Iot.Protocol;
using Npgsql;

var command = args.FirstOrDefault() ?? "prepare";
var secrets = Settings.SecretDirectory;
Directory.CreateDirectory(secrets);
if (command == "prepare")
{
    var countIndex = Array.IndexOf(args, "--count");
    var count = countIndex >= 0 ? int.Parse(args[countIndex + 1]) : 1000;
    if (count is < 1 or > 100000) throw new ArgumentOutOfRangeException(nameof(count));
    foreach (var name in new[] { "db-password", "api-key", "master-key", "cert-password" })
    {
        var path = Path.Combine(secrets, name);
        if (!File.Exists(path)) await File.WriteAllTextAsync(path, Convert.ToBase64String(RandomNumberGenerator.GetBytes(32)));
    }
    var manifestPath = Path.Combine(secrets, "devices.json");
    if (!File.Exists(manifestPath))
    {
        var devices = Enumerable.Range(1, count).Select(i => new SimulatorDevice("01", "01", i.ToString("D11"),
            (((i - 1) / 100) + 1).ToString("D9"), Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))));
        await File.WriteAllTextAsync(manifestPath, JsonSerializer.Serialize(devices, new JsonSerializerOptions { WriteIndented = true }));
    }
    if (!File.Exists(Path.Combine(secrets, "server.pfx")))
    {
        using var rsa = RSA.Create(3072);
        var request = new CertificateRequest("CN=localhost", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        var san = new SubjectAlternativeNameBuilder();
        san.AddDnsName("localhost"); san.AddDnsName("proxy"); san.AddDnsName("gateway");
        san.AddIpAddress(IPAddress.Loopback);
        var extra = Environment.GetEnvironmentVariable("TLS_DNS_NAME");
        if (!string.IsNullOrWhiteSpace(extra)) san.AddDnsName(extra);
        request.CertificateExtensions.Add(san.Build());
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, true));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, true));
        using var certificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-5), DateTimeOffset.UtcNow.AddMonths(6));
        await File.WriteAllBytesAsync(Path.Combine(secrets, "server.pfx"), certificate.Export(X509ContentType.Pfx, Settings.Secret("cert-password")));
        await File.WriteAllBytesAsync(Path.Combine(secrets, "server.cer"), certificate.Export(X509ContentType.Cert));
    }
    Console.WriteLine("Prepared local secrets and device manifest. Existing files were preserved; no keys printed.");
}
else if (command == "seed")
{
    await using var db = Settings.DataSource();
    await Schema.ApplyAsync(db);
    var protector = new DeviceKeyProtector(Convert.FromBase64String(Settings.Secret("master-key")));
    var devices = JsonSerializer.Deserialize<List<SimulatorDevice>>(await File.ReadAllTextAsync(Path.Combine(secrets, "devices.json")))!;
    await using var connection = await db.OpenConnectionAsync();
    await using var transaction = await connection.BeginTransactionAsync();
    foreach (var device in devices)
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO merchants(id,name) VALUES(@merchant,'Demo merchant') ON CONFLICT DO NOTHING;
            INSERT INTO devices(id,merchant_id,encrypted_key) VALUES(@device,@merchant,@key) ON CONFLICT DO NOTHING;
            INSERT INTO device_totals(device_id) VALUES(@device) ON CONFLICT DO NOTHING;
            """, connection, transaction);
        cmd.Parameters.AddWithValue("merchant", device.Merchant);
        cmd.Parameters.AddWithValue("device", device.Identity.Key);
        cmd.Parameters.AddWithValue("key", protector.Encrypt(Convert.FromBase64String(device.Key)));
        await cmd.ExecuteNonQueryAsync();
    }
    await transaction.CommitAsync();
    Console.WriteLine($"Schema ready; {devices.Count} demo device records provisioned or already present.");
}
else throw new ArgumentException("Expected prepare or seed.");
