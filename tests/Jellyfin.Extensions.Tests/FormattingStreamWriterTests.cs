using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace Jellyfin.Extensions.Tests;

public static class FormattingStreamWriterTests
{
    [Fact]
    public static void Shuffle_Valid_Correct()
    {
        Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE", false);
        using (var ms = new MemoryStream())
        using (var txt = new FormattingStreamWriter(ms, CultureInfo.InvariantCulture))
        {
            txt.Write("{0}", 3.14159);
            txt.Close();
            Assert.Equal("3.14159", Encoding.UTF8.GetString(ms.ToArray()));
        }
    }
}
