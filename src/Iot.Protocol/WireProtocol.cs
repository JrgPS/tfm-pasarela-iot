using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Iot.Protocol;

public sealed class ProtocolException(string code, string message) : Exception(message)
{
    public string Code { get; } = code;
}

public sealed record DeviceId(string Manufacturer, string Model, string Serial)
{
    public string Key => $"{Manufacturer}{Model}{Serial}";
    public bool IsValid => WireProtocol.Digits(Manufacturer, 2) &&
                           WireProtocol.Digits(Model, 2) && WireProtocol.Digits(Serial, 11);
}

public sealed record Request(DeviceId Device, DateTimeOffset Timestamp, string Action,
    string Id, string Payload, string Mac, string Canonical, string Frame);

public sealed record Response(DeviceId Device, string Timestamp, string Action, string Id,
    string Code, string Merchant, string Payload, string Mac, string Canonical);

public static class WireProtocol
{
    public const int MaxFrameBytes = 4096;
    public const string NoMerchant = "000000000";
    private const string DateFormat = "yyMMddHHmm";

    public static bool Digits(string value, int length) =>
        value.Length == length && value.All(c => c is >= '0' and <= '9');

    private static bool Hex(string value, int length) =>
        value.Length == length && value.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');

    public static string Date(DateTimeOffset timestamp) =>
        timestamp.UtcDateTime.ToString(DateFormat, CultureInfo.InvariantCulture);

    public static string NewId() => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(16));

    public static string Sign(string canonical, ReadOnlySpan<byte> key) =>
        Convert.ToHexStringLower(HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(canonical)));

    public static bool Verify(string canonical, string mac, ReadOnlySpan<byte> key) =>
        Hex(mac, 64) && CryptographicOperations.FixedTimeEquals(
            HMACSHA256.HashData(key, Encoding.ASCII.GetBytes(canonical)), Convert.FromHexString(mac));

    public static string CreateRequest(DeviceId device, string action, string payload,
        ReadOnlySpan<byte> key, DateTimeOffset? now = null, string? id = null)
    {
        var canonical = $"IOT1|Q|{device.Manufacturer}|{device.Model}|{device.Serial}|" +
                        $"{Date(now ?? DateTimeOffset.UtcNow)}|{action}|{id ?? NewId()}|{payload}|";
        var frame = canonical + Sign(canonical, key);
        _ = ParseRequest(frame);
        return frame;
    }

    public static Request ParseRequest(string frame)
    {
        ValidateFrame(frame);
        var f = frame.Split('|');
        if (f.Length != 10) throw new ProtocolException("003", "Expected ten fields.");
        if (f[0] != "IOT1") throw new ProtocolException("015", "Unsupported version.");
        var device = new DeviceId(f[2], f[3], f[4]);
        if (f[1] != "Q" || !device.IsValid || !Digits(f[6], 3) ||
            !Hex(f[7], 32) || !Hex(f[9], 64) || f[8].Length == 0)
            throw new ProtocolException("003", "Invalid request field.");
        var timestamp = ParseDate(f[5]);
        var canonical = frame[..(frame.LastIndexOf('|') + 1)];
        return new(device, timestamp, f[6], f[7], f[8], f[9], canonical, frame);
    }

    public static string CreateResponse(Request request, string code, string merchant, string payload,
        byte[]? key, DateTimeOffset? now = null) =>
        CreateResponse(request.Device, request.Action, request.Id, code, merchant, payload, key, now);

    public static string CreateResponse(DeviceId device, string action, string id, string code,
        string merchant, string payload, byte[]? key, DateTimeOffset? now = null)
    {
        var canonical = $"IOT1|R|{device.Manufacturer}|{device.Model}|{device.Serial}|" +
                        $"{Date(now ?? DateTimeOffset.UtcNow)}|{action}|{id}|{code}|{merchant}|{payload}|";
        var frame = canonical + (key is null ? "-" : Sign(canonical, key));
        _ = ParseResponse(frame);
        return frame;
    }

    public static Response ParseResponse(string frame)
    {
        ValidateFrame(frame);
        var f = frame.Split('|');
        if (f.Length != 12 || f[0] != "IOT1" || f[1] != "R")
            throw new ProtocolException("003", "Invalid response envelope.");
        var device = new DeviceId(f[2], f[3], f[4]);
        _ = ParseDate(f[5]);
        if (!device.IsValid || !Digits(f[6], 3) || !Hex(f[7], 32) ||
            !Digits(f[8], 3) || !Digits(f[9], 9) || f[10].Length == 0 ||
            (f[11] != "-" && !Hex(f[11], 64)))
            throw new ProtocolException("003", "Invalid response field.");
        return new(device, f[5], f[6], f[7], f[8], f[9], f[10], f[11],
            frame[..(frame.LastIndexOf('|') + 1)]);
    }

    public static string? Reject(string frame, string code)
    {
        var f = frame.Split('|');
        if (f.Length < 8) return null;
        var device = new DeviceId(f[2], f[3], f[4]);
        return device.IsValid && Digits(f[6], 3) && Hex(f[7], 32)
            ? CreateResponse(device, f[6], f[7], code, NoMerchant, "-", null) : null;
    }

    private static DateTimeOffset ParseDate(string value)
    {
        if (!Digits(value, 10) || !DateTimeOffset.TryParseExact("20" + value,
                "yyyyMMddHHmm", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
            throw new ProtocolException("007", "Invalid UTC date.");
        return date;
    }

    private static void ValidateFrame(string frame)
    {
        if (frame.Length == 0 || frame.Length + 1 > MaxFrameBytes || frame.Any(c => c is < '!' or > '~'))
            throw new ProtocolException("003", "Frame is not bounded printable ASCII.");
    }

    public static (long A, long B) ParseAddition(string payload)
    {
        var parts = payload.Split(',');
        if (parts.Length != 3 || parts[0] != "ADD")
            throw new ProtocolException("008", "Expected ADD and two operands.");
        return (Operand(parts[1]), Operand(parts[2]));
    }

    private static long Operand(string value)
    {
        if (value.Length != 12 || value[0] is not ('+' or '-') || !Digits(value[1..], 11) ||
            value == "-00000000000") throw new ProtocolException("008", "Invalid operand.");
        return long.Parse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
    }

    public static string Signed(decimal value) => (value < 0 ? "-" : "+") +
        decimal.Abs(value).ToString("0", CultureInfo.InvariantCulture);
}
