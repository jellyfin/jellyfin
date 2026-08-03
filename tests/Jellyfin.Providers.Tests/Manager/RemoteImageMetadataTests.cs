using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Model.Entities;
using MediaBrowser.Providers.Manager;
using Xunit;

namespace Jellyfin.Providers.Tests.Manager
{
    public class RemoteImageMetadataTests
    {
        private const string Url = "https://example.com/image.jpg";

        [Fact]
        public void GetSavedImage_ExplicitIndex_ReturnsThatImage()
        {
            var item = GetItemWithBackdrops(3);

            var image = RemoteImageMetadata.GetSavedImage(item, ImageType.Backdrop, 1);

            Assert.Same(item.GetImages(ImageType.Backdrop).ElementAt(1), image);
        }

        [Fact]
        public void GetSavedImage_SingularNullIndex_ReturnsFirstImage()
        {
            var item = new Movie();
            item.SetImage(new ItemImageInfo { Type = ImageType.Primary, Path = "/primary.jpg" }, 0);

            var image = RemoteImageMetadata.GetSavedImage(item, ImageType.Primary, null);

            Assert.Same(item.GetImageInfo(ImageType.Primary, 0), image);
        }

        [Fact]
        public void GetSavedImage_MultiImageNullIndex_ReturnsAppendedImage()
        {
            // A null index appends for multi-image types, so index 0 would be a pre-existing image.
            var item = GetItemWithBackdrops(3);

            var image = RemoteImageMetadata.GetSavedImage(item, ImageType.Backdrop, null);

            Assert.Same(item.GetImages(ImageType.Backdrop).Last(), image);
            Assert.NotSame(item.GetImages(ImageType.Backdrop).First(), image);
        }

        [Fact]
        public void GetSavedImage_MissingImage_ReturnsNull()
        {
            Assert.Null(RemoteImageMetadata.GetSavedImage(new Movie(), ImageType.Primary, null));
            Assert.Null(RemoteImageMetadata.GetSavedImage(new Movie(), ImageType.Backdrop, null));
            Assert.Null(RemoteImageMetadata.GetSavedImage(new Movie(), ImageType.Primary, 4));
        }

        [Fact]
        public void Record_MultiImageNullIndex_DoesNotTouchOtherImages()
        {
            var item = GetItemWithBackdrops(2);
            var first = item.GetImages(ImageType.Backdrop).First();

            RemoteImageMetadata.Record(
                RemoteImageMetadata.GetSavedImage(item, ImageType.Backdrop, null),
                Url,
                CreateResponse("\"abc\"", new DateTimeOffset(2024, 1, 2, 3, 4, 5, TimeSpan.Zero)));

            var last = item.GetImages(ImageType.Backdrop).Last();
            Assert.Equal(Url, last.Source);
            Assert.Equal("\"abc\"", last.ETag);
            Assert.Equal(new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc), last.SourceLastModified);

            Assert.Null(first.Source);
            Assert.Null(first.ETag);
            Assert.Null(first.SourceLastModified);
        }

        [Fact]
        public void Record_ResponseWithoutValidators_ClearsThem()
        {
            var item = new Movie();
            item.SetImage(
                new ItemImageInfo
                {
                    Type = ImageType.Primary,
                    Path = "/primary.jpg",
                    Source = "https://example.com/old.jpg",
                    ETag = "\"stale\"",
                    SourceLastModified = DateTime.UtcNow
                },
                0);

            RemoteImageMetadata.Record(item.GetImageInfo(ImageType.Primary, 0), Url, CreateResponse(null, null));

            var image = item.GetImageInfo(ImageType.Primary, 0);
            Assert.Equal(Url, image.Source);
            Assert.Null(image.ETag);
            Assert.Null(image.SourceLastModified);
        }

        [Fact]
        public void Record_NullImage_DoesNotThrow()
            => RemoteImageMetadata.Record(null, Url, CreateResponse("\"abc\"", null));

        private static BaseItem GetItemWithBackdrops(int count)
        {
            var item = new Movie();
            for (var i = 0; i < count; i++)
            {
                item.SetImage(
                    new ItemImageInfo
                    {
                        Type = ImageType.Backdrop,
                        Path = string.Create(System.Globalization.CultureInfo.InvariantCulture, $"/backdrop{i}.jpg")
                    },
                    i);
            }

            return item;
        }

        private static HttpResponseMessage CreateResponse(string? etag, DateTimeOffset? lastModified)
        {
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent(Array.Empty<byte>())
            };

            if (etag is not null)
            {
                response.Headers.ETag = new EntityTagHeaderValue(etag);
            }

            if (lastModified is not null)
            {
                response.Content.Headers.LastModified = lastModified;
            }

            return response;
        }
    }
}
