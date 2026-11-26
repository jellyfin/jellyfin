using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class TranscodeTestControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private static string? _accessToken;
    private readonly JellyfinApplicationFactory _factory;

    public TranscodeTestControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("99")]
    [InlineData("\"99\"")]
    [InlineData("\"UnknownTest\"")]
    public async Task StartTest_UndefinedKind_ReturnsBadRequestWithoutStarting(string kind)
    {
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        using var content = new StringContent("{\"Kind\":" + kind + "}", Encoding.UTF8, "application/json");
        using var response = await client.PostAsync("/Transcoding/Test", content, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("DELETE")]
    public async Task TranscodingTest_AnonymousRequest_IsDenied(string method)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), "/Transcoding/Test");
        if (method == "POST")
        {
            request.Content = new StringContent("{\"Kind\":0}", Encoding.UTF8, "application/json");
        }

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
