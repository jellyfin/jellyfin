using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.MediaInfo;
using Moq;
using Xunit;

namespace Jellyfin.Providers.Tests.MediaInfo
{
    public class EmbeddedImageProviderTests
    {
        [Theory]
        [InlineData(typeof(AudioBook))]
        [InlineData(typeof(BoxSet))]
        [InlineData(typeof(Series))]
        [InlineData(typeof(Season))]
        [InlineData(typeof(Episode), ImageType.Primary)]
        [InlineData(typeof(Movie), ImageType.Logo, ImageType.Backdrop, ImageType.Primary)]
        public void GetSupportedImages_AnyBaseItem_ReturnsExpected(Type type, params ImageType[] expected)
        {
            BaseItem item = (BaseItem)Activator.CreateInstance(type)!;
            var embeddedImageProvider = new EmbeddedImageProvider(Mock.Of<IMediaEncoder>());
            var actual = embeddedImageProvider.GetSupportedImages(item);
            Assert.Equal(expected.OrderBy(i => i.ToString()), actual.OrderBy(i => i.ToString()));
        }

        [Fact]
        public async void GetImage_NoStreams_ReturnsNoImage()
        {
            var embeddedImageProvider = new EmbeddedImageProvider(null);

            var input = GetMovie(new List<MediaAttachment>(), new List<MediaStream>());

            var actual = await embeddedImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Theory]
        [InlineData("unmatched", null, 1, ImageType.Primary, null)] // doesn't default on no match
        [InlineData("clearlogo.png", null, 1, ImageType.Logo, ImageFormat.Png)] // extract extension from name
        [InlineData("backdrop", "image/bmp", 2, ImageType.Backdrop, ImageFormat.Bmp)] // extract extension from mimetype
        [InlineData("poster", null, 3, ImageType.Primary, ImageFormat.Jpg)] // default extension to jpg
        public async void GetImage_Attachment_ReturnsCorrectSelection(string filename, string mimetype, int targetIndex, ImageType type, ImageFormat? format)
        {
            var attachments = new List<MediaAttachment>();
            string pathPrefix = "path";
            for (int i = 1; i <= targetIndex; i++)
            {
                var name = i == targetIndex ? filename : "unmatched";
                attachments.Add(new ()
                {
                    FileName = name,
                    MimeType = mimetype,
                    Index = i
                });
            }

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<int>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, MediaSourceInfo, MediaStream, int, ImageFormat, CancellationToken>((_, _, _, _, index, ext, _) => Task.FromResult(pathPrefix + index + "." + ext));
            var embeddedImageProvider = new EmbeddedImageProvider(mediaEncoder.Object);

            var input = GetMovie(attachments, new List<MediaStream>());

            var actual = await embeddedImageProvider.GetImage(input, type, CancellationToken.None);
            Assert.NotNull(actual);
            if (format == null)
            {
                Assert.False(actual.HasImage);
            }
            else
            {
                Assert.True(actual.HasImage);
                Assert.Equal(pathPrefix + targetIndex + "." + format, actual.Path, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(format, actual.Format);
            }
        }

        [Theory]
        [InlineData(null, 1, ImageType.Backdrop, false)] // no label, can only find primary
        [InlineData(null, 1, ImageType.Primary, true)] // no label, finds primary
        [InlineData("backdrop", 2, ImageType.Backdrop, true)] // uses label to find index 2, not just pulling first stream
        [InlineData("cover", 2, ImageType.Primary, true)] // uses label to find index 2, not just pulling first stream
        public async void GetImage_Embedded_ReturnsCorrectSelection(string label, int targetIndex, ImageType type, bool hasImage)
        {
            var streams = new List<MediaStream>();
            for (int i = 1; i <= targetIndex; i++)
            {
                var comment = i == targetIndex ? label : "unmatched";
                streams.Add(new ()
                {
                    Type = MediaStreamType.EmbeddedImage,
                    Index = i,
                    Comment = comment
                });
            }

            var pathPrefix = "path";
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<int>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, MediaSourceInfo, MediaStream, int, ImageFormat, CancellationToken>((_, _, _, stream, index, ext, _) =>
                {
                    Assert.Equal(streams[index - 1], stream);
                    return Task.FromResult(pathPrefix + index + "." + ext);
                });
            var embeddedImageProvider = new EmbeddedImageProvider(mediaEncoder.Object);

            var input = GetMovie(new List<MediaAttachment>(), streams);

            var actual = await embeddedImageProvider.GetImage(input, type, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.Equal(hasImage, actual.HasImage);
            if (hasImage)
            {
                Assert.Equal(pathPrefix + targetIndex + ".jpg", actual.Path, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(ImageFormat.Jpg, actual.Format);
            }
        }

        private static Movie GetMovie(List<MediaAttachment> mediaAttachments, List<MediaStream> mediaStreams)
        {
            // Mocking IMediaSourceManager GetMediaAttachments and GetMediaStreams instead of mocking Movie works, but
            // has concurrency problems between this and VideoImageProviderTests due to BaseItem.MediaSourceManager
            // being static
            var movie = new Mock<Movie>();

            movie.Setup(item => item.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo> { new () { MediaAttachments = mediaAttachments } } );
            movie.Setup(item => item.GetMediaStreams())
                .Returns(mediaStreams);

            return movie.Object;
        }
    }
}
