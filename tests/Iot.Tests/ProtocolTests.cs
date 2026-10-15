using System.Security.Cryptography;
using System.Text;
using Iot.Protocol;
using Xunit;

namespace Iot.Tests;

public class ProtocolTests
{
    private static readonly byte[] Key = Enumerable.Range(0, 32).Select(i => (byte)i).ToArray();
    private static readonly DeviceId Device = new("01", "02", "00000000001");

    [Fact]
    public void HmacMatchesIndependentRfc4231Vector()
    {
        var key = Enumerable.Repeat((byte)0x0b, 20).ToArray();
        Assert.Equal("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7",
            WireProtocol.Sign("Hi There", key));
    }

    [Fact]
    public void MessageRoundtripPreservesLeadingZerosAndDetectsTampering()
    {
        var frame = WireProtocol.CreateRequest(Device, "001", "-", Key,
            new DateTimeOffset(2069, 12, 31, 23, 59, 0, TimeSpan.Zero));
        var request = WireProtocol.ParseRequest(frame);
        Assert.Equal(Device, request.Device);
        Assert.Equal(2069, request.Timestamp.Year);
        Assert.True(WireProtocol.Verify(request.Canonical, request.Mac, Key));
        Assert.False(WireProtocol.Verify(request.Canonical.Replace("|001|", "|003|"), request.Mac, Key));
    }

    [Theory]
    [InlineData("ADD,+00000001234,-00000000567", 667L)]
    [InlineData("ADD,+99999999999,+99999999999", 199999999998L)]
    [InlineData("ADD,-99999999999,+99999999999", 0L)]
    public void AdditionUsesExactScaledIntegers(string payload, long expected)
    {
        var (a, b) = WireProtocol.ParseAddition(payload);
        Assert.Equal(expected, checked(a + b));
    }

    [Theory]
    [InlineData("ADD,-00000000000,+00000000001")]
    [InlineData("ADD,00000000001,+00000000001")]
    [InlineData("ADD,+00000000１23,+00000000001")]
    [InlineData("ADD,+000000001.2,+00000000001")]
    [InlineData("SUB,+00000000001,+00000000001")]
    public void InvalidOperandsAreRejected(string payload) =>
        Assert.Equal("008", Assert.Throws<ProtocolException>(() => WireProtocol.ParseAddition(payload)).Code);

    [Fact]
    public async Task TcpFragmentationAndCoalescingDoNotChangeMessages()
    {
        var first = WireProtocol.CreateRequest(Device, "001", "-", Key);
        var second = WireProtocol.CreateRequest(Device, "003", "-", Key);
        using var stream = new FragmentedStream(Encoding.ASCII.GetBytes(first + "\n" + second + "\n"));
        var reader = new FrameReader(stream);
        Assert.Equal(first, await reader.ReadAsync());
        Assert.Equal(second, await reader.ReadAsync());
        Assert.Null(await reader.ReadAsync());
    }

    [Theory]
    [InlineData("hello\r\n")]
    [InlineData("hello")]
    [InlineData("hello\u0080\n")]
    public async Task InvalidTransportBytesAndTruncatedFramesAreRejected(string text)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
        await Assert.ThrowsAsync<ProtocolException>(() => new FrameReader(stream).ReadAsync());
    }

    [Fact]
    public async Task FrameLimitIncludesLf()
    {
        using var accepted = new MemoryStream(Encoding.ASCII.GetBytes(new string('A', 4095) + "\n"));
        Assert.Equal(4095, (await new FrameReader(accepted).ReadAsync())!.Length);
        using var rejected = new MemoryStream(Encoding.ASCII.GetBytes(new string('A', 4096) + "\n"));
        await Assert.ThrowsAsync<ProtocolException>(() => new FrameReader(rejected).ReadAsync());
    }

    private sealed class FragmentedStream(byte[] bytes) : MemoryStream(bytes)
    {
        private int reads;
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken token = default) =>
            base.ReadAsync(buffer[..Math.Min(buffer.Length, ++reads % 3 == 0 ? 8192 : 1)], token);
    }
}
