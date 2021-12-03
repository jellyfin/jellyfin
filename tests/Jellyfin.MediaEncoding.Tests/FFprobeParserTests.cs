using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Extensions.Json;
using MediaBrowser.MediaEncoding.Probing;
using MediaBrowser.Model.IO;
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
            await using (var stream = AsyncFile.OpenRead(path))
            {
                var res = await JsonSerializer.DeserializeAsync<InternalMediaInfoResult>(stream, JsonDefaults.Options).ConfigureAwait(false);
                Assert.NotNull(res);
            }
        }
    }
}
