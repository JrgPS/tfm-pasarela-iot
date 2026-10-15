using System.Security.Cryptography;
using Iot.Persistence;
using Iot.Protocol;
using Iot.Registry;
using Npgsql;
using Xunit;

namespace Iot.Tests;

public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("IOT_TEST_CONNECTION")))
            Skip = "Set IOT_TEST_CONNECTION to a disposable PostgreSQL iot_tests database.";
    }
}

public sealed class PostgresTests
{
    [PostgresFact]
    public async Task InitializationAdditionAndTotalsAreDurable()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Initialize();
        var sum = await test.Send(connection, "002", "ADD,+00000001234,-00000000567");
        Assert.Equal("000", sum.Code); Assert.Equal("SUM,+667", sum.Payload);
        Assert.Equal("123456789", sum.Merchant);
        Assert.True(WireProtocol.Verify(sum.Canonical, sum.Mac, test.Key));
        // Use a different store instance to prove no process-local totals are required.
        test.Store = new RegistryStore(test.Database, test.Protector);
        var totals = await test.Send(connection, "003", "-");
        Assert.Equal("TOTALS,1,+667", totals.Payload);
        await using var query = test.Database.CreateCommand("SELECT count(*) FROM operations WHERE device_id=@device");
        query.Parameters.AddWithValue("device", test.Device.Key);
        Assert.Equal(1L, await query.ExecuteScalarAsync());
    }

    [PostgresFact]
    public async Task ConcurrentReplaysAcrossRegistryInstancesExecuteOnlyOnce()
    {
        await using var test = await TestStore.Create();
        var connections = new[] { await test.Initialize(), await test.Initialize(), await test.Initialize() };
        var original = WireProtocol.CreateRequest(test.Device, "002", "ADD,+00000000001,+00000000002", test.Key);
        var store2 = new RegistryStore(test.Database, test.Protector);
        var tasks = Enumerable.Range(0, 50).Select(i => (i % 2 == 0 ? test.Store : store2).ProcessAsync(
            new(connections[i % 3], "test", Guid.NewGuid().ToString(), original), CancellationToken.None));
        var results = await Task.WhenAll(tasks);
        Assert.Single(results.Select(r => r.Frame).Distinct());
        Assert.Equal("TOTALS,1,+3", (await test.Send(connections[0], "003", "-")).Payload);
    }

    [PostgresFact]
    public async Task ConcurrentDistinctRequestsDoNotLoseUpdates()
    {
        await using var test = await TestStore.Create();
        var connections = new[] { await test.Initialize(), await test.Initialize() };
        await Task.WhenAll(Enumerable.Range(0, 100).Select(i => test.Send(connections[i % 2], "002",
            "ADD,+00000000001,+00000000002")));
        Assert.Equal("TOTALS,100,+300", (await test.Send(connections[0], "003", "-")).Payload);
    }

    [PostgresFact]
    public async Task ReusedIdentifierWithChangedPayloadCannotMutateTotals()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Initialize();
        var id = WireProtocol.NewId();
        await test.Send(connection, "002", "ADD,+00000000001,+00000000002", id: id);
        Assert.Equal("012", (await test.Send(connection, "002", "ADD,+00000000003,+00000000004", id: id)).Code);
        Assert.Equal("TOTALS,1,+3", (await test.Send(connection, "003", "-")).Payload);
    }

    [PostgresFact]
    public async Task InvalidMacDoesNotDiscloseMerchantOrChangeTotals()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Initialize();
        var response = await test.Send(connection, "002", "ADD,+00000000001,+00000000002", key: new byte[32]);
        Assert.Equal("005", response.Code); Assert.Equal("000000000", response.Merchant); Assert.Equal("-", response.Mac);
        Assert.Equal("TOTALS,0,+0", (await test.Send(connection, "003", "-")).Payload);
    }

    [PostgresFact]
    public async Task UnknownDeviceIsAuditedWithoutADeviceForeignKey()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Open();
        var frame = WireProtocol.CreateRequest(new("99", "99", "99999999999"), "001", "-", test.Key);
        var result = await test.Store.ProcessAsync(new(connection, "test", Guid.NewGuid().ToString(), frame), CancellationToken.None);
        Assert.Equal("002", WireProtocol.ParseResponse(result.Frame!).Code);
        await using var query = test.Database.CreateCommand("SELECT count(*) FROM message_attempts WHERE connection_id=@id");
        query.Parameters.AddWithValue("id", Guid.Parse(connection));
        Assert.Equal(1L, await query.ExecuteScalarAsync());
    }

    [PostgresFact]
    public async Task SessionIsRequiredAndBlockedDeviceCannotKeepOperating()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Open();
        Assert.Equal("010", (await test.Send(connection, "003", "-")).Code);
        Assert.Equal("000", (await test.Send(connection, "001", "-")).Code);
        await using var cmd = test.Database.CreateCommand("UPDATE devices SET active=false WHERE id=@id");
        cmd.Parameters.AddWithValue("id", test.Device.Key); await cmd.ExecuteNonQueryAsync();
        Assert.Equal("004", (await test.Send(connection, "003", "-")).Code);
    }

    [PostgresFact]
    public async Task StaleRequestUnsupportedActionAndBadOperandsAreDenied()
    {
        await using var test = await TestStore.Create();
        var connection = await test.Initialize();
        Assert.Equal("007", (await test.Send(connection, "003", "-", timestamp: DateTimeOffset.UtcNow.AddMinutes(-10))).Code);
        Assert.Equal("006", (await test.Send(connection, "999", "-")).Code);
        Assert.Equal("008", (await test.Send(connection, "002", "ADD,1,2")).Code);
        Assert.Equal("TOTALS,0,+0", (await test.Send(connection, "003", "-")).Payload);
    }

    private sealed class TestStore : IAsyncDisposable
    {
        public required NpgsqlDataSource Database { get; init; }
        public required DeviceKeyProtector Protector { get; init; }
        public required RegistryStore Store { get; set; }
        public byte[] Key { get; } = RandomNumberGenerator.GetBytes(32);
        public DeviceId Device { get; } = new("98", "01", RandomNumberGenerator.GetInt32(1, int.MaxValue).ToString("D11"));

        public static async Task<TestStore> Create()
        {
            var connectionString = Environment.GetEnvironmentVariable("IOT_TEST_CONNECTION")!;
            if (!new NpgsqlConnectionStringBuilder(connectionString).Database!.EndsWith("_tests", StringComparison.Ordinal))
                throw new InvalidOperationException("Integration tests require a dedicated database ending in _tests.");
            var db = NpgsqlDataSource.Create(connectionString);
            await Schema.ApplyAsync(db);
            var protector = new DeviceKeyProtector(RandomNumberGenerator.GetBytes(32));
            var test = new TestStore { Database = db, Protector = protector, Store = new(db, protector) };
            await using var cmd = db.CreateCommand("""
                INSERT INTO merchants(id,name) VALUES('123456789','Integration test') ON CONFLICT DO NOTHING;
                INSERT INTO devices(id,merchant_id,encrypted_key) VALUES(@device,'123456789',@key);
                INSERT INTO device_totals(device_id) VALUES(@device);
                """);
            cmd.Parameters.AddWithValue("device", test.Device.Key);
            cmd.Parameters.AddWithValue("key", protector.Encrypt(test.Key));
            await cmd.ExecuteNonQueryAsync();
            return test;
        }

        public async Task<string> Open()
        {
            var id = Guid.NewGuid().ToString();
            await Store.OpenAsync(new(id, "test"), CancellationToken.None);
            return id;
        }
        public async Task<string> Initialize()
        {
            var id = await Open();
            Assert.Equal("000", (await Send(id, "001", "-")).Code);
            return id;
        }
        public async Task<Response> Send(string connection, string action, string payload, string? id = null,
            byte[]? key = null, DateTimeOffset? timestamp = null)
        {
            var frame = WireProtocol.CreateRequest(Device, action, payload, key ?? Key, timestamp, id);
            var result = await Store.ProcessAsync(new(connection, "test", Guid.NewGuid().ToString(), frame), CancellationToken.None);
            return WireProtocol.ParseResponse(result.Frame!);
        }
        public ValueTask DisposeAsync() => Database.DisposeAsync();
    }
}
