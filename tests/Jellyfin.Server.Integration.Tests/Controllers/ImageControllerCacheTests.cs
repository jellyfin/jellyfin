using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Jellyfin.Drawing;
using MediaBrowser.Controller.Drawing;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers
{
    public sealed class ImageControllerCacheTests : IClassFixture<JellyfinApplicationFactory>
    {
        private readonly JellyfinApplicationFactory _factory;
        private static string? _accessToken;

        public ImageControllerCacheTests(JellyfinApplicationFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task GetUserImage_ParallelRequestsForUncachedImage_AllReceiveCompleteImage()
        {
            Assert.SkipWhen(
                _factory.Services.GetRequiredService<IImageEncoder>() is NullImageEncoder,
                "Requires an image encoder (Skia native library not available).");

            var client = _factory.CreateClient();
            client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
            var userId = (await AuthHelper.GetUserDtoAsync(client)).Id;

            var noisePngBase64 = Convert.ToBase64String(GenerateNoisePng(1600, 2400));

            for (var round = 0; round < 2; round++)
            {
                using (var content = new StringContent(noisePngBase64))
                {
                    content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
                    using var uploadResponse = await client.PostAsync($"UserImage?userId={userId}", content, TestContext.Current.CancellationToken);
                    Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);
                }

                foreach (var format in new[] { "Jpg", "Webp" })
                {
                    var url = $"UserImage?userId={userId}&format={format}";

                    var responses = await Task.WhenAll(Enumerable.Range(0, 40)
                        .Select(_ => client.GetAsync(url, TestContext.Current.CancellationToken)));

                    try
                    {
                        Assert.All(responses, r => Assert.Equal(HttpStatusCode.OK, r.StatusCode));

                        var bodyLengths = await Task.WhenAll(responses.Select(async r =>
                            (await r.Content.ReadAsByteArrayAsync(TestContext.Current.CancellationToken)).Length));

                        Assert.All(bodyLengths, length => Assert.NotEqual(0, length));
                        Assert.Single(bodyLengths.Distinct());
                    }
                    finally
                    {
                        foreach (var response in responses)
                        {
                            response.Dispose();
                        }
                    }
                }
            }
        }

        private static byte[] GenerateNoisePng(int width, int height)
        {
            using var bitmap = new SKBitmap(width, height);
            var pixelBytes = new byte[bitmap.ByteCount];
            new Random(17135).NextBytes(pixelBytes);
            Marshal.Copy(pixelBytes, 0, bitmap.GetPixels(), pixelBytes.Length);

            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return data.ToArray();
        }
    }
}
