using System.Buffers;
using System.Text;

namespace Iot.Protocol;

/// <summary>Reads LF frames without assuming TCP read boundaries are message boundaries.</summary>
public sealed class FrameReader(Stream stream, TimeSpan? idleTimeout = null, TimeSpan? frameTimeout = null)
{
    private readonly byte[] buffer = new byte[8192];
    private int offset;
    private int available;

    public async Task<string?> ReadAsync(CancellationToken cancellationToken = default)
    {
        var frame = new ArrayBufferWriter<byte>();
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(idleTimeout ?? TimeSpan.FromSeconds(120));
        var started = false;
        while (true)
        {
            if (offset == available)
            {
                available = await stream.ReadAsync(buffer, deadline.Token);
                offset = 0;
                if (available == 0)
                {
                    if (frame.WrittenCount != 0) throw new ProtocolException("003", "Truncated frame.");
                    return null;
                }
            }
            if (!started)
            {
                started = true;
                deadline.CancelAfter(frameTimeout ?? TimeSpan.FromSeconds(10));
            }
            while (offset < available)
            {
                var b = buffer[offset++];
                if (b == 10) return Encoding.ASCII.GetString(frame.WrittenSpan);
                if (b is < 33 or > 126) throw new ProtocolException("003", "Non-ASCII byte or whitespace.");
                if (frame.WrittenCount >= WireProtocol.MaxFrameBytes - 1)
                    throw new ProtocolException("003", "Frame exceeds limit.");
                frame.GetSpan(1)[0] = b;
                frame.Advance(1);
            }
        }
    }
}
