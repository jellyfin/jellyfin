using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.MediaInfo;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class VideoImageProviderTests
    {
        public static TheoryData<Video> GetImage_UnsupportedInput_ReturnsNoImage_TestData()
        {
            return new()
            {
                new Movie { IsPlaceHolder = true },

                new Movie { DefaultVideoStreamIndex = null },

                // set a default index but don't put anything there (invalid input, but provider shouldn't break)
                new Movie { DefaultVideoStreamIndex = 0 }
            };
        }

        [Theory]
        [MemberData(nameof(GetImage_UnsupportedInput_ReturnsNoImage_TestData))]
        public async Task GetImage_UnsupportedInput_ReturnsNoImage(Video input)
        {
            var mediaSourceManager = GetMediaSourceManager(input, null, new List<MediaStream>());
            var videoImageProvider = new VideoImageProvider(mediaSourceManager, Mock.Of<IMediaEncoder>(), new NullLogger<VideoImageProvider>());

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Theory]
        [InlineData(1, 1)] // default not first stream
        [InlineData(5, 0)] // default out of valid range
        public async Task GetImage_DefaultVideoStreams_ReturnsCorrectStreamImage(int defaultIndex, int targetIndex)
        {
            var input = new Movie { DefaultVideoStreamIndex = defaultIndex };

            string targetPath = "path.jpg";
            var mediaStreams = new List<MediaStream>();
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);

            for (int i = 0; i <= targetIndex; i++)
            {
                var mediaStream = new MediaStream { Type = MediaStreamType.Video, Index = i };
                mediaStreams.Add(mediaStream);

                var path = i == targetIndex ? targetPath : "wrong stream called!";
                mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), mediaStream, It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), It.IsAny<CancellationToken>()))
                    .Returns(Task.FromResult(path));
            }

            var defaultStream = defaultIndex < mediaStreams.Count ? mediaStreams[targetIndex] : null;
            var mediaSourceManager = GetMediaSourceManager(input, defaultStream, mediaStreams);

            var videoImageProvider = new VideoImageProvider(mediaSourceManager, mediaEncoder.Object, new NullLogger<VideoImageProvider>());

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Theory]
        [InlineData(null, 10)] // default time
        [InlineData(500, 50)] // calculated time
        public async Task GetImage_TimeSpan_SelectsCorrectTime(int? runTimeSeconds, long expectedSeconds)
        {
            MediaStream targetStream = new() { Type = MediaStreamType.Video, Index = 0 };
            var input = new Movie
            {
                DefaultVideoStreamIndex = 0,
                RunTimeTicks = runTimeSeconds * TimeSpan.TicksPerSecond
            };

            var mediaSourceManager = GetMediaSourceManager(input, targetStream, new List<MediaStream> { targetStream });

            // use a callback to catch the actual value
            // provides more information on failure than verifying a specific input was called on the mock
            TimeSpan? actualTimeSpan = null;
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Callback<string, string, MediaSourceInfo, MediaStream, Video3DFormat?, TimeSpan?, CancellationToken>((_, _, _, _, _, timeSpan, _) => actualTimeSpan = timeSpan)
                .Returns(Task.FromResult("path"));

            var videoImageProvider = new VideoImageProvider(mediaSourceManager, mediaEncoder.Object, new NullLogger<VideoImageProvider>());

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), actualTimeSpan);
        }

        private static IMediaSourceManager GetMediaSourceManager(Video item, MediaStream? defaultStream, List<MediaStream> mediaStreams)
        {
            var defaultStreamList = new List<MediaStream>();
            if (defaultStream is not null)
            {
                defaultStreamList.Add(defaultStream);
            }

            var mediaSourceManager = new Mock<IMediaSourceManager>(MockBehavior.Strict);
            mediaSourceManager.Setup(i => i.GetMediaStreams(It.Is<MediaStreamQuery>(q => q.ItemId.Equals(item.Id) && q.Index == item.DefaultVideoStreamIndex)))
                .Returns(defaultStreamList);
            mediaSourceManager.Setup(i => i.GetMediaStreams(It.Is<MediaStreamQuery>(q => q.ItemId.Equals(item.Id) && q.Type == MediaStreamType.Video)))
                .Returns(mediaStreams);
            return mediaSourceManager.Object;
        }
    }
}
