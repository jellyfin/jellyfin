using System.IO;
using System.Text.Json;
using Xunit;

namespace Jellyfin.MediaEncoding.Keyframes.FfProbe
{
    public class FfProbeKeyframeExtractorTests
    {
        [Theory]
        [InlineData("keyframes.txt", "keyframes_result.json")]
        [InlineData("keyframes_streamduration.txt", "keyframes_streamduration_result.json")]
        public void ParseStream_Valid_Success(string testDataFileName, string resultFileName)
        {
            var testDataPath = Path.Combine("FfProbe/Test Data", testDataFileName);
            var resultPath = Path.Combine("FfProbe/Test Data", resultFileName);
            using var resultFileStream = File.OpenRead(resultPath);
            var expectedResult = JsonSerializer.Deserialize<KeyframeData>(resultFileStream)!;

            using var fileStream = File.OpenRead(testDataPath);
            using var streamReader = new StreamReader(fileStream);

            var result = FfProbeKeyframeExtractor.ParseStream(streamReader);

            Assert.Equal(expectedResult.TotalDuration, result.TotalDuration);
            Assert.Equal(expectedResult.KeyframeTicks, result.KeyframeTicks);
        }
    }
}
