using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
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
            var embeddedImageProvider = new EmbeddedImageProvider(Mock.Of<IMediaSourceManager>(), Mock.Of<IMediaEncoder>(), new NullLogger<EmbeddedImageProvider>());
            var actual = embeddedImageProvider.GetSupportedImages(item);
            Assert.Equal(expected.OrderBy(i => i.ToString()), actual.OrderBy(i => i.ToString()));
        }

        [Fact]
        public async Task GetImage_NoStreams_ReturnsNoImage()
        {
            var input = new Movie();

            var mediaSourceManager = GetMediaSourceManager(input, new List<MediaAttachment>(), new List<MediaStream>());
            var embeddedImageProvider = new EmbeddedImageProvider(mediaSourceManager, null, new NullLogger<EmbeddedImageProvider>());

            var actual = await embeddedImageProvider.GetImage(input, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Theory]
        [InlineData("chapter", null, 1, ImageType.Chapter, null)] // unexpected type, nothing found
        [InlineData("unmatched", null, 1, ImageType.Primary, null)] // doesn't default on no match
        [InlineData("clearlogo.png", null, 1, ImageType.Logo, ImageFormat.Png)] // extract extension from name
        [InlineData("backdrop", "image/bmp", 2, ImageType.Backdrop, ImageFormat.Bmp)] // extract extension from mimetype
        [InlineData("poster", null, 3, ImageType.Primary, ImageFormat.Jpg)] // default extension to jpg
        public async Task GetImage_Attachment_ReturnsCorrectSelection(string filename, string? mimetype, int targetIndex, ImageType type, ImageFormat? expectedFormat)
        {
            var attachments = new List<MediaAttachment>();
            string pathPrefix = "path";
            for (int i = 1; i <= targetIndex; i++)
            {
                var name = i == targetIndex ? filename : "unmatched";
                attachments.Add(new()
                {
                    FileName = name,
                    MimeType = mimetype,
                    Index = i
                });
            }

            var input = new Movie();

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<int>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, MediaSourceInfo, MediaStream, int, ImageFormat, CancellationToken>((_, _, _, _, index, ext, _) => Task.FromResult(pathPrefix + index + "." + ext));
            var mediaSourceManager = GetMediaSourceManager(input, attachments, new List<MediaStream>());
            var embeddedImageProvider = new EmbeddedImageProvider(mediaSourceManager, mediaEncoder.Object, new NullLogger<EmbeddedImageProvider>());

            var actual = await embeddedImageProvider.GetImage(input, type, CancellationToken.None);
            Assert.NotNull(actual);
            if (expectedFormat is null)
            {
                Assert.False(actual.HasImage);
            }
            else
            {
                Assert.True(actual.HasImage);
                Assert.Equal(pathPrefix + targetIndex + "." + expectedFormat, actual.Path, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedFormat, actual.Format);
            }
        }

        [Theory]
        [InlineData("chapter", null, 1, ImageType.Chapter, null)] // unexpected type, nothing found
        [InlineData(null, null, 1, ImageType.Backdrop, null)] // no label, can only find primary
        [InlineData(null, null, 1, ImageType.Primary, ImageFormat.Jpg)] // no label, finds primary
        [InlineData("backdrop", null, 2, ImageType.Backdrop, ImageFormat.Jpg)] // uses label to find index 2, not just pulling first stream
        [InlineData("cover", null, 2, ImageType.Primary, ImageFormat.Jpg)] // uses label to find index 2, not just pulling first stream
        [InlineData(null, "bmp", 1, ImageType.Primary, ImageFormat.Bmp)]
        [InlineData(null, "gif", 1, ImageType.Primary, ImageFormat.Gif)]
        [InlineData(null, "mjpeg", 1, ImageType.Primary, ImageFormat.Jpg)]
        [InlineData(null, "png", 1, ImageType.Primary, ImageFormat.Png)]
        [InlineData(null, "webp", 1, ImageType.Primary, ImageFormat.Webp)]
        public async Task GetImage_Embedded_ReturnsCorrectSelection(string? label, string? codec, int targetIndex, ImageType type, ImageFormat? expectedFormat)
        {
            var streams = new List<MediaStream>();
            for (int i = 1; i <= targetIndex; i++)
            {
                var comment = i == targetIndex ? label : "unmatched";
                streams.Add(new()
                {
                    Type = MediaStreamType.EmbeddedImage,
                    Index = i,
                    Comment = comment,
                    Codec = codec
                });
            }

            var input = new Movie();

            var pathPrefix = "path";
            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), It.IsAny<int>(), It.IsAny<ImageFormat>(), It.IsAny<CancellationToken>()))
                .Returns<string, string, MediaSourceInfo, MediaStream, int, ImageFormat, CancellationToken>((_, _, _, stream, index, ext, _) =>
                {
                    Assert.Equal(streams[index - 1], stream);
                    return Task.FromResult(pathPrefix + index + "." + ext);
                });
            var mediaSourceManager = GetMediaSourceManager(input, new List<MediaAttachment>(), streams);
            var embeddedImageProvider = new EmbeddedImageProvider(mediaSourceManager, mediaEncoder.Object, new NullLogger<EmbeddedImageProvider>());

            var actual = await embeddedImageProvider.GetImage(input, type, CancellationToken.None);
            Assert.NotNull(actual);
            if (expectedFormat is null)
            {
                Assert.False(actual.HasImage);
            }
            else
            {
                Assert.True(actual.HasImage);
                Assert.Equal(pathPrefix + targetIndex + "." + expectedFormat, actual.Path, StringComparer.OrdinalIgnoreCase);
                Assert.Equal(expectedFormat, actual.Format);
            }
        }

        private static IMediaSourceManager GetMediaSourceManager(BaseItem item, List<MediaAttachment> mediaAttachments, List<MediaStream> mediaStreams)
        {
            var mediaSourceManager = new Mock<IMediaSourceManager>(MockBehavior.Strict);
            mediaSourceManager.Setup(i => i.GetMediaAttachments(item.Id))
                .Returns(mediaAttachments);
            mediaSourceManager.Setup(i => i.GetMediaStreams(It.Is<MediaStreamQuery>(q => q.ItemId.Equals(item.Id) && q.Type == MediaStreamType.EmbeddedImage)))
                .Returns(mediaStreams);
            return mediaSourceManager.Object;
        }
    }
}
