using System.IO;
using ATL;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo;

public class AtlFlacRegressionTests
{
    [Fact(Timeout = 5000)]
    public void AtlParser_MalformedFlac_DoesNotHang()
    {
        // The 456-byte Vorbis comment block claims a 1024-byte vendor string.
        byte[] fixture = [
            (byte)'f', (byte)'L', (byte)'a', (byte)'C',
            0, 0, 0, 34,
            .. new byte[34],
            0x84, 0, 1, 200,
            0, 4, 0, 0,
            .. new byte[452]
        ];

        // ATL 7.16 hangs on this stream.
        using var stream = new MemoryStream(fixture);
        _ = new Track(stream, ".flac");
    }
}
