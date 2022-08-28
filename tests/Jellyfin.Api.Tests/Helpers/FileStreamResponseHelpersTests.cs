using Jellyfin.Api.Helpers;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class FileStreamResponseHelpersTests
    {
        [Fact]
        public static async void RewriteUrisInM3UPlaylist_Success()
        {
            FileStreamResponseHelpers.SegmentUriHmacKey = "someKey";
            var inputPlaylist = @"#EXTM3U
#EXT-X-TARGETDURATION:10
#EXT-X-KEY:METHOD=AES-128,URI=""http://media.example.com/key"",IV=someIV
#EXT-X-MAP:URI=""/init.mp4""
# Some comment

#EXTINF:9.009,
first.ts
#EXTINF:9.009,
//media.example.com/second.ts
#EXTINF:3.003,
https://media.example.com/third.ts";
            var expectedOutput = @"#EXTM3U
#EXT-X-TARGETDURATION:10
#EXT-X-KEY:METHOD=""AES-128"",URI=""/Videos/someItemId/stream?static=true&mediaSourceId=someMediaSourceId&api_key=someAccessToken&segmentUri=http%3A%2F%2Fmedia.example.com%2Fkey&segmentToken=8RYTjz61eGPNqNJm4c7ebrqxJxi6XsXQ9fdpI6R0eLk%3D"",IV=""someIV""
#EXT-X-MAP:URI=""/Videos/someItemId/stream.mp4?static=true&mediaSourceId=someMediaSourceId&api_key=someAccessToken&segmentUri=%2Finit.mp4&segmentToken=EFzS%2Fdnu2VjC7IkwQce0YEJiQPDYGujVHQjGxLRf2xo%3D""
# Some comment

#EXTINF:9.009,
/Videos/someItemId/stream.ts?static=true&mediaSourceId=someMediaSourceId&api_key=someAccessToken&segmentUri=first.ts&segmentToken=4SQyEq01QE77FCYwCZk1MVL2XpzfneVeem7%2FMUkK3zo%3D
#EXTINF:9.009,
/Videos/someItemId/stream.ts?static=true&mediaSourceId=someMediaSourceId&api_key=someAccessToken&segmentUri=%2F%2Fmedia.example.com%2Fsecond.ts&segmentToken=JIyCHnuYGfiwKMPZ3DzxONI9oDyZIIbJNkarh2tYAkg%3D
#EXTINF:3.003,
/Videos/someItemId/stream.ts?static=true&mediaSourceId=someMediaSourceId&api_key=someAccessToken&segmentUri=https%3A%2F%2Fmedia.example.com%2Fthird.ts&segmentToken=m4TCMYK0lXtEzNZAfQkYErEK8gAaAoZcqeXIy5qnpDc%3D";
            var result = await FileStreamResponseHelpers.RewriteUrisInM3UPlaylist("someItemId", "someMediaSourceId", "someAccessToken", inputPlaylist);
            Assert.Equal(expectedOutput, result.Content);
        }
    }
}
