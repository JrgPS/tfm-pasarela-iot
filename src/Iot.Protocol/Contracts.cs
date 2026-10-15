namespace Iot.Protocol;

public sealed record ConnectionEvent(string ConnectionId, string Gateway, string? Reason = null);
public sealed record ProcessMessage(string ConnectionId, string Gateway, string EventId, string Frame);
public sealed record ProcessResult(string? Frame, bool Close = false);
public sealed record AuditRejection(string ConnectionId, string Gateway, string EventId, string Code, string Detail);
public sealed record SimulatorDevice(string Manufacturer, string Model, string Serial, string Merchant, string Key)
{
    public DeviceId Identity => new(Manufacturer, Model, Serial);
}
