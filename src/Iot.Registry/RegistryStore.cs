using System.Globalization;
using System.Security.Cryptography;
using Iot.Persistence;
using Iot.Protocol;
using Npgsql;

namespace Iot.Registry;

public sealed class RegistryStore(NpgsqlDataSource source, DeviceKeyProtector protector)
{
    public async Task OpenAsync(ConnectionEvent e, CancellationToken token)
    {
        await using var cmd = source.CreateCommand("""
            INSERT INTO connections(id,gateway) VALUES(@id,@gateway) ON CONFLICT(id) DO NOTHING
            """);
        cmd.Parameters.AddWithValue("id", Guid.Parse(e.ConnectionId));
        cmd.Parameters.AddWithValue("gateway", e.Gateway[..Math.Min(e.Gateway.Length, 128)]);
        await cmd.ExecuteNonQueryAsync(token);
    }

    public async Task ActivityAsync(ConnectionEvent e, CancellationToken token)
    {
        await using var cmd = source.CreateCommand("UPDATE connections SET last_activity_at=now() WHERE id=@id");
        cmd.Parameters.AddWithValue("id", Guid.Parse(e.ConnectionId));
        await cmd.ExecuteNonQueryAsync(token);
    }

    public async Task CloseAsync(ConnectionEvent e, CancellationToken token)
    {
        await using var cmd = source.CreateCommand("""
            UPDATE connections SET closed_at=COALESCE(closed_at,now()),
              close_reason=COALESCE(close_reason,@reason) WHERE id=@id
            """);
        cmd.Parameters.AddWithValue("id", Guid.Parse(e.ConnectionId));
        cmd.Parameters.AddWithValue("reason", e.Reason ?? "closed");
        await cmd.ExecuteNonQueryAsync(token);
    }

    public async Task RejectAsync(AuditRejection e, CancellationToken token)
    {
        await using var conn = await source.OpenConnectionAsync(token);
        await using var tx = await conn.BeginTransactionAsync(token);
        await Attempt(conn, tx, new(e.ConnectionId, e.Gateway, e.EventId, ""), null,
            e.Code, false, e.Detail[..Math.Min(e.Detail.Length, 4096)], token);
        await tx.CommitAsync(token);
    }

    public async Task<ProcessResult> ProcessAsync(ProcessMessage message, CancellationToken token)
    {
        Request? request = null;
        string? parseCode = null;
        try { request = WireProtocol.ParseRequest(message.Frame); }
        catch (ProtocolException ex) { parseCode = ex.Code; }

        await using var connection = await source.OpenConnectionAsync(token);
        await using var tx = await connection.BeginTransactionAsync(token);
        if (request is null)
        {
            await Attempt(connection, tx, message, null, parseCode!, false, "Malformed frame", token);
            await tx.CommitAsync(token);
            return new(WireProtocol.Reject(message.Frame, parseCode!), true);
        }

        // Serialize per device across all registry replicas; no process-local business state.
        string? merchant = null;
        byte[]? encryptedKey = null;
        var active = false;
        await using (var device = new NpgsqlCommand(
            "SELECT merchant_id, active, encrypted_key FROM devices WHERE id=@id FOR UPDATE", connection, tx))
        {
            device.Parameters.AddWithValue("id", request.Device.Key);
            await using var reader = await device.ExecuteReaderAsync(token);
            if (await reader.ReadAsync(token))
            { merchant = reader.GetString(0); active = reader.GetBoolean(1); encryptedKey = (byte[])reader[2]; }
        }
        byte[]? key = encryptedKey is null ? null : protector.Decrypt(encryptedKey);
        try
        {
            if (key is null) return await Finish("002", "-", false, null, WireProtocol.NoMerchant);
            if (!WireProtocol.Verify(request.Canonical, request.Mac, key))
                return await Finish("005", "-", true, null, WireProtocol.NoMerchant);
            if (!active) return await Finish("004", "-", true, key, WireProtocol.NoMerchant);

            string? sessionDevice;
            await using (var cmd = new NpgsqlCommand("SELECT device_id FROM connections WHERE id=@id FOR UPDATE", connection, tx))
            {
                cmd.Parameters.AddWithValue("id", Guid.Parse(message.ConnectionId));
                var value = await cmd.ExecuteScalarAsync(token);
                sessionDevice = value is string s ? s : null;
            }
            if (sessionDevice is not null && sessionDevice != request.Device.Key)
                return await Finish("011", "-", true, key, merchant!);
            if (sessionDevice is null && request.Action != "001")
                return await Finish("010", "-", false, key, merchant!);

            string? oldCanonical = null, oldResponse = null;
            await using (var old = new NpgsqlCommand(
                "SELECT canonical,response FROM requests WHERE device_id=@device AND request_id=@id", connection, tx))
            {
                old.Parameters.AddWithValue("device", request.Device.Key);
                old.Parameters.AddWithValue("id", request.Id);
                await using var reader = await old.ExecuteReaderAsync(token);
                if (await reader.ReadAsync(token)) { oldCanonical = reader.GetString(0); oldResponse = reader.GetString(1); }
            }
            if (oldResponse is not null)
            {
                if (oldCanonical != request.Canonical) return await Finish("012", "-", false, key, merchant!);
                if (request.Action == "001" && WireProtocol.ParseResponse(oldResponse).Code == "000")
                    await BindSession();
                await Attempt(connection, tx, message, request, WireProtocol.ParseResponse(oldResponse).Code,
                    true, null, token);
                await tx.CommitAsync(token);
                return new(oldResponse);
            }
            if ((DateTimeOffset.UtcNow - request.Timestamp).Duration() > TimeSpan.FromMinutes(5))
                return await Finish("007", "-", false, key, merchant!);

            string payload;
            switch (request.Action)
            {
                case "001":
                    if (request.Payload != "-") return await Finish("008", "-", false, key, merchant!);
                    await BindSession();
                    payload = "READY";
                    break;
                case "002":
                    long a, b;
                    try { (a, b) = WireProtocol.ParseAddition(request.Payload); }
                    catch (ProtocolException) { return await Finish("008", "-", false, key, merchant!); }
                    var sum = checked(a + b);
                    // The device row lock also serializes total updates. Check bounds before SQL.
                    var (count, accumulated) = await ReadTotals();
                    const decimal limit = 9999999999999999999999999999m;
                    if (count == long.MaxValue || accumulated + sum > limit || accumulated + sum < -limit)
                        return await Finish("014", "-", false, key, merchant!);
                    await using (var op = new NpgsqlCommand("""
                        INSERT INTO operations(device_id,request_id,operand_a,operand_b,result)
                        VALUES(@device,@id,@a,@b,@sum);
                        UPDATE device_totals SET operation_count=operation_count+1,result_sum=result_sum+@sum
                        WHERE device_id=@device
                        """, connection, tx))
                    {
                        op.Parameters.AddWithValue("device", request.Device.Key);
                        op.Parameters.AddWithValue("id", request.Id);
                        op.Parameters.AddWithValue("a", a); op.Parameters.AddWithValue("b", b);
                        op.Parameters.AddWithValue("sum", sum);
                        await op.ExecuteNonQueryAsync(token);
                    }
                    payload = "SUM," + WireProtocol.Signed(sum);
                    break;
                case "003":
                    if (request.Payload != "-") return await Finish("008", "-", false, key, merchant!);
                    var totals = await ReadTotals();
                    payload = $"TOTALS,{totals.Count.ToString(CultureInfo.InvariantCulture)},{WireProtocol.Signed(totals.Sum)}";
                    break;
                default: return await Finish("006", "-", false, key, merchant!);
            }
            var response = WireProtocol.CreateResponse(request, "000", merchant!, payload, key);
            await using (var save = new NpgsqlCommand("""
                INSERT INTO requests(device_id,request_id,canonical,response) VALUES(@device,@id,@canonical,@response)
                """, connection, tx))
            {
                save.Parameters.AddWithValue("device", request.Device.Key);
                save.Parameters.AddWithValue("id", request.Id);
                save.Parameters.AddWithValue("canonical", request.Canonical);
                save.Parameters.AddWithValue("response", response);
                await save.ExecuteNonQueryAsync(token);
            }
            await Attempt(connection, tx, message, request, "000", false, null, token);
            await tx.CommitAsync(token);
            return new(response);

            async Task BindSession()
            {
                await using var cmd = new NpgsqlCommand(
                    "UPDATE connections SET device_id=@device,last_activity_at=now() WHERE id=@id", connection, tx);
                cmd.Parameters.AddWithValue("device", request.Device.Key);
                cmd.Parameters.AddWithValue("id", Guid.Parse(message.ConnectionId));
                await cmd.ExecuteNonQueryAsync(token);
            }

            async Task<(long Count, decimal Sum)> ReadTotals()
            {
                await using var cmd = new NpgsqlCommand(
                    "SELECT operation_count,result_sum FROM device_totals WHERE device_id=@device", connection, tx);
                cmd.Parameters.AddWithValue("device", request.Device.Key);
                await using var reader = await cmd.ExecuteReaderAsync(token);
                if (!await reader.ReadAsync(token)) throw new InvalidOperationException("Device totals missing.");
                return (reader.GetInt64(0), reader.GetDecimal(1));
            }
            async Task<ProcessResult> Finish(string code, string result, bool close, byte[]? responseKey, string responseMerchant)
            {
                await Attempt(connection, tx, message, request, code, false, null, token);
                var frame = WireProtocol.CreateResponse(request, code, responseMerchant, result, responseKey);
                await tx.CommitAsync(token);
                return new(frame, close);
            }
        }
        finally { if (key is not null) CryptographicOperations.ZeroMemory(key); }
    }

    private static async Task Attempt(NpgsqlConnection connection, NpgsqlTransaction tx, ProcessMessage message,
        Request? request, string code, bool duplicate, string? detail, CancellationToken token)
    {
        await using var cmd = new NpgsqlCommand("""
            INSERT INTO message_attempts(event_id,connection_id,device_claim,request_id,action,code,duplicate,canonical,detail)
            VALUES(@event,@connection,@device,@id,@action,@code,@duplicate,@canonical,@detail)
            ON CONFLICT(event_id) DO NOTHING
            """, connection, tx);
        cmd.Parameters.AddWithValue("event", Guid.Parse(message.EventId));
        cmd.Parameters.AddWithValue("connection", Guid.Parse(message.ConnectionId));
        cmd.Parameters.AddWithValue("device", (object?)request?.Device.Key ?? DBNull.Value);
        cmd.Parameters.AddWithValue("id", (object?)request?.Id ?? DBNull.Value);
        cmd.Parameters.AddWithValue("action", (object?)request?.Action ?? DBNull.Value);
        cmd.Parameters.AddWithValue("code", code);
        cmd.Parameters.AddWithValue("duplicate", duplicate);
        // Full payload for valid messages, excluding the replayable MAC. Malformed input is bounded.
        cmd.Parameters.AddWithValue("canonical", request?.Canonical ??
            message.Frame[..Math.Min(message.Frame.Length, 4095)]);
        cmd.Parameters.AddWithValue("detail", (object?)detail ?? DBNull.Value);
        await cmd.ExecuteNonQueryAsync(token);
    }
}
