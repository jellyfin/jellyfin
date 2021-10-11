using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
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
        [Fact]
        public async void GetImage_InputIsPlaceholder_ReturnsNoImage()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var input = new Movie
            {
                IsPlaceHolder = true
            };

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_NoDefaultVideoStream_ReturnsNoImage()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var input = new Movie
            {
                DefaultVideoStreamIndex = null
            };

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_DefaultSetButNoVideoStream_ReturnsNoImage()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            // set a default index but don't put anything there (invalid input, but provider shouldn't break)
            var input = GetMovie(0, null, new List<MediaStream>());

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_DefaultSetMultipleVideoStreams_ReturnsDefaultStreamImage()
        {
            MediaStream firstStream = new () { Type = MediaStreamType.Video, Index = 0 };
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 1 };
            string targetPath = "path.jpg";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), firstStream, It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Returns(Task.FromResult("wrong stream called!"));
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), targetStream, It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Returns(Task.FromResult(targetPath));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = GetMovie(1, targetStream, new List<MediaStream> { firstStream, targetStream } );

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Fact]
        public async void GetImage_InvalidDefaultSingleVideoStream_ReturnsFirstVideoStreamImage()
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };
            string targetPath = "path.jpg";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), targetStream, It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Returns(Task.FromResult(targetPath));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            // provide query results for default (empty) and all streams (populated)
            var input = GetMovie(5, null, new List<MediaStream> { targetStream });

            var actual = await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Fact]
        public async void GetImage_NoTimeSpanSet_CallsEncoderWithDefaultTime()
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

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromSeconds(10), actualTimeSpan);
        }

        [Fact]
        public async void GetImage_TimeSpanSet_CallsEncoderWithCalculatedTime()
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };

            TimeSpan? actualTimeSpan = null;
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Callback<string, string, MediaSourceInfo, MediaStream, Video3DFormat?, TimeSpan?, CancellationToken>((_, _, _, _, _, timeSpan, _) => actualTimeSpan = timeSpan)
                .Returns(Task.FromResult("path"));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = GetMovie(0, targetStream, new List<MediaStream> { targetStream });
            input.RunTimeTicks = 5000;

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromTicks(500), actualTimeSpan);
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
            movie.Setup(item => item.GetMediaStreams())
                .Returns(mediaStreams);

            return movie.Object;
        }
    }
}
