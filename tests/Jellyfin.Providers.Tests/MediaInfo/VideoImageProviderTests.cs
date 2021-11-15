using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.MediaEncoding;
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
        private static TheoryData<Video> GetImage_UnsupportedInput_ReturnsNoImage_TestData()
        {
            return new ()
            {
                new Movie { IsPlaceHolder = true },

                new Movie { DefaultVideoStreamIndex = null },

                // set a default index but don't put anything there (invalid input, but provider shouldn't break)
                GetMovie(0, null, new List<MediaStream>())
            };
        }

        [Theory]
        [MemberData(nameof(GetImage_UnsupportedInput_ReturnsNoImage_TestData))]
        public async void GetImage_UnsupportedInput_ReturnsNoImage(Video input)
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Theory]
        [InlineData(1, 1)] // default not first stream
        [InlineData(5, 0)] // default out of valid range
        public async void GetImage_DefaultVideoStreams_ReturnsCorrectStreamImage(int defaultIndex, int targetIndex)
        {
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

            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var defaultStream = defaultIndex < mediaStreams.Count ? mediaStreams[targetIndex] : null;
            var input = GetMovie(defaultIndex, defaultStream, mediaStreams );

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Theory]
        [InlineData(null, 10)] // default time
        [InlineData(500, 50)] // calculated time
        public async void GetImage_TimeSpan_SelectsCorrectTime(int? runTimeSeconds, long expectedSeconds)
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };

            // use a callback to catch the actual value
            // provides more information on failure than verifying a specific input was called on the mock
            TimeSpan? actualTimeSpan = null;
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Callback<string, string, MediaSourceInfo, MediaStream, Video3DFormat?, TimeSpan?, CancellationToken>((_, _, _, _, _, timeSpan, _) => actualTimeSpan = timeSpan)
                .Returns(Task.FromResult("path"));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = GetMovie(0, targetStream, new List<MediaStream> { targetStream });
            input.RunTimeTicks = runTimeSeconds * TimeSpan.TicksPerSecond;

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromSeconds(expectedSeconds), actualTimeSpan);
        }

        private static VideoImageProvider GetVideoImageProvider(IMediaEncoder? mediaEncoder)
        {
            // strict to ensure this isn't accidentally used where a prepared mock is intended
            mediaEncoder ??= new Mock<IMediaEncoder>(MockBehavior.Strict).Object;
            return new VideoImageProvider(mediaEncoder, new NullLogger<VideoImageProvider>());
        }

        private static Movie GetMovie(int defaultVideoStreamIndex, MediaStream? defaultStream, List<MediaStream> mediaStreams)
        {
            // Mocking IMediaSourceManager GetMediaStreams instead of mocking Movie works, but has concurrency problems
            // between this and EmbeddedImageProviderTests due to BaseItem.MediaSourceManager being static
            var movie = new Mock<Movie>
            {
                Object =
                {
                    DefaultVideoStreamIndex = defaultVideoStreamIndex
                }
            };

            movie.Setup(item => item.GetDefaultVideoStream())
                .Returns(defaultStream!);
            movie.Setup(item => item.GetMediaStreams(MediaStreamType.Video))
                .Returns(mediaStreams);

            return movie.Object;
        }
    }
}
