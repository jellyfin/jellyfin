using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Branding;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class ImageControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private const string SplashscreenUrl = "/Branding/Splashscreen";
    private static string? _accessToken;
    private readonly JellyfinApplicationFactory _factory;

    public ImageControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("image/png")] // mislabeled body
    [InlineData("image/tiff")] // maps to an extension, so it clears the mime check; body still is not an image
    public async Task UploadCustomSplashscreen_UndecodableBody_BadRequestAndConfigUntouched(string contentType)
    {
        var client = await GetAuthenticatedClientAsync();
        var before = GetBrandingOptions().SplashscreenLocation;

        var notAnImage = Encoding.UTF8.GetBytes("this is not an image");
        using var content = new StringContent(Convert.ToBase64String(notAnImage), Encoding.UTF8, contentType);
        using var response = await client.PostAsync(SplashscreenUrl, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, GetBrandingOptions().SplashscreenLocation);
    }

    [Fact]
    public async Task UploadCustomSplashscreen_UnknownImageMimeType_BadRequest()
    {
        // image/jxl (the format from the issue) has no extension mapping, so it is rejected at the mime step.
        var client = await GetAuthenticatedClientAsync();
        var before = GetBrandingOptions().SplashscreenLocation;

        using var content = new StringContent(Convert.ToBase64String(CreatePng()), Encoding.UTF8, "image/jxl");
        using var response = await client.PostAsync(SplashscreenUrl, content, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(before, GetBrandingOptions().SplashscreenLocation);
    }

    [Fact]
    public async Task UploadCustomSplashscreen_ValidPng_SavedAndServed()
    {
        var client = await GetAuthenticatedClientAsync();

        using var content = new StringContent(Convert.ToBase64String(CreatePng()), Encoding.UTF8, "image/png");
        using var uploadResponse = await client.PostAsync(SplashscreenUrl, content, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);

        var location = GetBrandingOptions().SplashscreenLocation;
        Assert.NotNull(location);
        Assert.True(File.Exists(location));

        using var getResponse = await client.GetAsync(SplashscreenUrl, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.StartsWith("image/", getResponse.Content.Headers.ContentType?.MediaType, StringComparison.Ordinal);
    }

    [Fact]
    public async Task UpdateBrandingConfiguration_ViaNamedConfigurationRoute_PreservesSplashscreenLocation()
    {
        // jellyfin-web saves the branding form by POSTing to /System/Configuration/branding with a DTO
        // that has no SplashscreenLocation. The literal Configuration/Branding route must win over
        // Configuration/{key}, otherwise the location is wiped on every save (issue step 3-4).
        var client = await GetAuthenticatedClientAsync();

        using var upload = new StringContent(Convert.ToBase64String(CreatePng()), Encoding.UTF8, "image/png");
        using var uploadResponse = await client.PostAsync(SplashscreenUrl, upload, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, uploadResponse.StatusCode);
        var location = GetBrandingOptions().SplashscreenLocation;
        Assert.NotNull(location);

        var dto = new BrandingOptionsDto { LoginDisclaimer = "hello", CustomCss = null, SplashscreenEnabled = true };
        using var saveResponse = await client.PostAsJsonAsync("/System/Configuration/branding", dto, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.NoContent, saveResponse.StatusCode);

        var after = GetBrandingOptions();
        Assert.Equal(location, after.SplashscreenLocation);
        Assert.Equal("hello", after.LoginDisclaimer);
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        return client;
    }

    private BrandingOptions GetBrandingOptions()
        => _factory.Services.GetRequiredService<IServerConfigurationManager>().GetConfiguration<BrandingOptions>("branding");

    private static byte[] CreatePng()
    {
        using var bitmap = new SKBitmap(2, 2);
        bitmap.Erase(SKColors.Red);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
