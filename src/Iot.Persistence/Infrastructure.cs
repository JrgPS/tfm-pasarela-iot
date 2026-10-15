using System.Security.Cryptography;
using Npgsql;

namespace Iot.Persistence;

public static class Settings
{
    public static string SecretDirectory => Environment.GetEnvironmentVariable("IOT_SECRETS") ??
                                             Path.Combine(Environment.CurrentDirectory, "secrets");
    public static string Secret(string name) => File.ReadAllText(Path.Combine(SecretDirectory, name)).Trim();
    public static NpgsqlDataSource DataSource()
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost",
            Port = int.Parse(Environment.GetEnvironmentVariable("DB_PORT") ?? "5432"),
            Database = Environment.GetEnvironmentVariable("DB_NAME") ?? "iot",
            Username = Environment.GetEnvironmentVariable("DB_USER") ?? "iot",
            Password = Secret("db-password"),
            MaxPoolSize = 20, Timeout = 5, CommandTimeout = 5,
            IncludeErrorDetail = false
        };
        return NpgsqlDataSource.Create(builder.ConnectionString);
    }
}

public sealed class DeviceKeyProtector(byte[] masterKey)
{
    public byte[] Encrypt(byte[] plain)
    {
        var envelope = new byte[12 + 16 + plain.Length];
        RandomNumberGenerator.Fill(envelope.AsSpan(0, 12));
        using var aes = new AesGcm(masterKey, 16);
        aes.Encrypt(envelope.AsSpan(0, 12), plain, envelope.AsSpan(28), envelope.AsSpan(12, 16));
        return envelope;
    }

    public byte[] Decrypt(byte[] envelope)
    {
        if (envelope.Length != 60) throw new CryptographicException("Invalid device key envelope.");
        var plain = new byte[32];
        using var aes = new AesGcm(masterKey, 16);
        aes.Decrypt(envelope.AsSpan(0, 12), envelope.AsSpan(28), envelope.AsSpan(12, 16), plain);
        return plain;
    }
}

public static class Schema
{
    public static async Task ApplyAsync(NpgsqlDataSource source, CancellationToken token = default)
    {
        await using var connection = await source.OpenConnectionAsync(token);
        // A single migration entry point, serialized even if accidentally launched twice.
        await using var transaction = await connection.BeginTransactionAsync(token);
        await using var gate = new NpgsqlCommand("SELECT pg_advisory_xact_lock(84201701)", connection, transaction);
        await gate.ExecuteNonQueryAsync(token);
        using var stream = typeof(Schema).Assembly.GetManifestResourceStream("Iot.Persistence.schema.sql")!;
        using var reader = new StreamReader(stream);
        await using var command = new NpgsqlCommand(await reader.ReadToEndAsync(token), connection, transaction);
        await command.ExecuteNonQueryAsync(token);
        await transaction.CommitAsync(token);
    }
}
