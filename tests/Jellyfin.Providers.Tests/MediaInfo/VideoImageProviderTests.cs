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
        private VideoImageProvider GetVideoImageProvider(IMediaEncoder? mediaEncoder)
        {
            // strict to ensure this isn't accidentally used where a prepared mock is intended
            mediaEncoder ??= new Mock<IMediaEncoder>(MockBehavior.Strict).Object;
            return new VideoImageProvider(mediaEncoder, new NullLogger<VideoImageProvider>());
        }

        [Fact]
        public async void GetImage_Empty_IsPlaceholder()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var input = new Mock<Movie>();
            input.Object.IsPlaceHolder = true;

            var actual = await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Empty_NoDefaultVideoStream()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var input = new Mock<Movie>();

            var actual = await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Empty_DefaultSet_NoVideoStream()
        {
            var videoImageProvider = GetVideoImageProvider(null);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>());
            // set a default index but don't put anything there (invalid input, but provider shouldn't break)
            input.Object.DefaultVideoStreamIndex = 1;

            var actual = await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Extract_DefaultStream()
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

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetDefaultVideoStream())
                .Returns(targetStream);
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>() { firstStream, targetStream });
            input.Object.DefaultVideoStreamIndex = 1;

            var actual = await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Fact]
        public async void GetImage_Extract_FallbackToFirstVideoStream()
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };
            string targetPath = "path.jpg";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), targetStream, It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Returns(Task.FromResult(targetPath));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>() { targetStream });
            // default must be set, ensure a stream is still found if not pointed at a video
            input.Object.DefaultVideoStreamIndex = 5;

            var actual = await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Fact]
        public async void GetImage_Time_Default()
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };

            TimeSpan? actualTimeSpan = null;
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Callback<string, string, MediaSourceInfo, MediaStream, Video3DFormat?, TimeSpan?, CancellationToken>((_, _, _, _, _, timeSpan, _) => actualTimeSpan = timeSpan)
                .Returns(Task.FromResult("path"));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>() { targetStream });
            // default must be set
            input.Object.DefaultVideoStreamIndex = 0;

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromSeconds(10), actualTimeSpan);
        }

        [Fact]
        public async void GetImage_Time_Calculated()
        {
            MediaStream targetStream = new () { Type = MediaStreamType.Video, Index = 0 };

            TimeSpan? actualTimeSpan = null;
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<Video3DFormat?>(), It.IsAny<TimeSpan?>(), CancellationToken.None))
                .Callback<string, string, MediaSourceInfo, MediaStream, Video3DFormat?, TimeSpan?, CancellationToken>((_, _, _, _, _, timeSpan, _) => actualTimeSpan = timeSpan)
                .Returns(Task.FromResult("path"));
            var videoImageProvider = GetVideoImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>() { targetStream });
            // default must be set
            input.Object.DefaultVideoStreamIndex = 0;
            input.Object.RunTimeTicks = 5000;

            // not testing return, just verifying what gets requested for time span
            await videoImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);

            Assert.Equal(TimeSpan.FromTicks(500), actualTimeSpan);
        }
    }
}
