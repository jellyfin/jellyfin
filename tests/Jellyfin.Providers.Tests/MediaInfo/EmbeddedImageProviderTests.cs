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
        public static TheoryData<BaseItem> GetSupportedImages_Empty_TestData =>
            new ()
            {
                new AudioBook(),
                new BoxSet(),
                new Series(),
                new Season(),
            };

        public static TheoryData<BaseItem, IEnumerable<ImageType>> GetSupportedImages_Populated_TestData =>
            new TheoryData<BaseItem, IEnumerable<ImageType>>
            {
                { new Episode(), new List<ImageType> { ImageType.Primary } },
                { new Movie(), new List<ImageType> { ImageType.Logo, ImageType.Backdrop, ImageType.Primary } },
            };

        private EmbeddedImageProvider GetEmbeddedImageProvider(IMediaEncoder? mediaEncoder)
        {
            return new EmbeddedImageProvider(mediaEncoder);
        }

        [Theory]
        [MemberData(nameof(GetSupportedImages_Empty_TestData))]
        public void GetSupportedImages_Empty(BaseItem item)
        {
            var embeddedImageProvider = GetEmbeddedImageProvider(null);
            Assert.False(embeddedImageProvider.GetSupportedImages(item).Any());
        }

        [Theory]
        [MemberData(nameof(GetSupportedImages_Populated_TestData))]
        public void GetSupportedImages_Populated(BaseItem item, IEnumerable<ImageType> expected)
        {
            var embeddedImageProvider = GetEmbeddedImageProvider(null);
            var actual = embeddedImageProvider.GetSupportedImages(item);
            Assert.Equal(expected.OrderBy(i => i.ToString()), actual.OrderBy(i => i.ToString()));
        }

        [Fact]
        public async void GetImage_Empty_NoStreams()
        {
            var embeddedImageProvider = GetEmbeddedImageProvider(null);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo>());
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>());

            var actual = await embeddedImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Empty_NoLabeledAttachments()
        {
            var embeddedImageProvider = GetEmbeddedImageProvider(null);

            var input = new Mock<Movie>();
            // add an attachment without a filename - has a list to look through but finds nothing
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo> { new () { MediaAttachments = new List<MediaAttachment> { new () } } });
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>());

            var actual = await embeddedImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Empty_NoEmbeddedLabeledBackdrop()
        {
            var embeddedImageProvider = GetEmbeddedImageProvider(null);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo>());
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream> { new () { Type = MediaStreamType.EmbeddedImage } });

            var actual = await embeddedImageProvider.GetImage(input.Object, ImageType.Backdrop, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.False(actual.HasImage);
        }

        [Fact]
        public async void GetImage_Attached()
        {
            // first tests file extension detection, second uses mimetype, third defaults to jpg
            MediaAttachment sampleAttachment1 = new () { FileName = "clearlogo.png", Index = 1 };
            MediaAttachment sampleAttachment2 = new () { FileName = "backdrop", MimeType = "image/bmp", Index = 2 };
            MediaAttachment sampleAttachment3 = new () { FileName = "poster", Index = 3 };
            string targetPath1 = "path1.png";
            string targetPath2 = "path2.bmp";
            string targetPath3 = "path2.jpg";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), 1, ".png", CancellationToken.None))
                .Returns(Task.FromResult(targetPath1));
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), 2, ".bmp", CancellationToken.None))
                .Returns(Task.FromResult(targetPath2));
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), It.IsAny<MediaStream>(), 3, ".jpg", CancellationToken.None))
                .Returns(Task.FromResult(targetPath3));
            var embeddedImageProvider = GetEmbeddedImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo> { new () { MediaAttachments = new List<MediaAttachment> { sampleAttachment1, sampleAttachment2, sampleAttachment3 } } });
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>());

            var actualLogo = await embeddedImageProvider.GetImage(input.Object, ImageType.Logo, CancellationToken.None);
            Assert.NotNull(actualLogo);
            Assert.True(actualLogo.HasImage);
            Assert.Equal(targetPath1, actualLogo.Path);
            Assert.Equal(ImageFormat.Png, actualLogo.Format);

            var actualBackdrop = await embeddedImageProvider.GetImage(input.Object, ImageType.Backdrop, CancellationToken.None);
            Assert.NotNull(actualBackdrop);
            Assert.True(actualBackdrop.HasImage);
            Assert.Equal(targetPath2, actualBackdrop.Path);
            Assert.Equal(ImageFormat.Bmp, actualBackdrop.Format);

            var actualPrimary = await embeddedImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actualPrimary);
            Assert.True(actualPrimary.HasImage);
            Assert.Equal(targetPath3, actualPrimary.Path);
            Assert.Equal(ImageFormat.Jpg, actualPrimary.Format);
        }

        [Fact]
        public async void GetImage_EmbeddedDefault()
        {
            MediaStream sampleStream = new () { Type = MediaStreamType.EmbeddedImage, Index = 1 };
            string targetPath = "path";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), sampleStream, 1, "jpg", CancellationToken.None))
                .Returns(Task.FromResult(targetPath));
            var embeddedImageProvider = GetEmbeddedImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo>());
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream>() { sampleStream });

            var actual = await embeddedImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actual);
            Assert.True(actual.HasImage);
            Assert.Equal(targetPath, actual.Path);
            Assert.Equal(ImageFormat.Jpg, actual.Format);
        }

        [Fact]
        public async void GetImage_EmbeddedSelection()
        {
            // primary is second stream to ensure it's not defaulting, backdrop is first
            MediaStream sampleStream1 = new () { Type = MediaStreamType.EmbeddedImage, Index = 1, Comment = "backdrop" };
            MediaStream sampleStream2 = new () { Type = MediaStreamType.EmbeddedImage, Index = 2, Comment = "cover" };
            string targetPath1 = "path1.jpg";
            string targetPath2 = "path2.jpg";

            var mediaEncoder = new Mock<IMediaEncoder>(MockBehavior.Strict);
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), sampleStream1, 1, "jpg", CancellationToken.None))
                .Returns(Task.FromResult(targetPath1));
            mediaEncoder.Setup(encoder => encoder.ExtractVideoImage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MediaSourceInfo>(), sampleStream2, 2, "jpg", CancellationToken.None))
                .Returns(Task.FromResult(targetPath2));
            var embeddedImageProvider = GetEmbeddedImageProvider(mediaEncoder.Object);

            var input = new Mock<Movie>();
            input.Setup(movie => movie.GetMediaSources(It.IsAny<bool>()))
                .Returns(new List<MediaSourceInfo>());
            input.Setup(movie => movie.GetMediaStreams())
                .Returns(new List<MediaStream> { sampleStream1, sampleStream2 });

            var actualPrimary = await embeddedImageProvider.GetImage(input.Object, ImageType.Primary, CancellationToken.None);
            Assert.NotNull(actualPrimary);
            Assert.True(actualPrimary.HasImage);
            Assert.Equal(targetPath2, actualPrimary.Path);
            Assert.Equal(ImageFormat.Jpg, actualPrimary.Format);

            var actualBackdrop = await embeddedImageProvider.GetImage(input.Object, ImageType.Backdrop, CancellationToken.None);
            Assert.NotNull(actualBackdrop);
            Assert.True(actualBackdrop.HasImage);
            Assert.Equal(targetPath1, actualBackdrop.Path);
            Assert.Equal(ImageFormat.Jpg, actualBackdrop.Format);
        }
    }
}
