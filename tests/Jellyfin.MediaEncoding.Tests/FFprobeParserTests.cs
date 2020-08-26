using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using MediaBrowser.MediaEncoding.Probing;
using Xunit;

namespace Jellyfin.MediaEncoding.Tests
{
    public class FFprobeParserTests
    {
        [Theory]
        [InlineData("ffprobe1.json")]
        public async Task Test(string fileName)
        {
            var path = Path.Join("Test Data", fileName);
            using (var stream = File.OpenRead(path))
            {
                await JsonSerializer.DeserializeAsync<InternalMediaInfoResult>(stream).ConfigureAwait(false);
            }
        }
    }
}
