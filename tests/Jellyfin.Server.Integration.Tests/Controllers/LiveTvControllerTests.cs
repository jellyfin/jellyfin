using System.Net;
using System.Threading.Tasks;
using Jellyfin.Extensions;
using Xunit;

namespace Jellyfin.Server.Integration.Tests.Controllers;

public sealed class LiveTvControllerTests : IClassFixture<JellyfinApplicationFactory>
{
    private readonly JellyfinApplicationFactory _factory;

    public LiveTvControllerTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task LiveTvIsUnavailable_WhileGenericChannelsRemain()
    {
        var client = _factory.CreateClient();
        var liveTvResponse = await client.GetAsync("/LiveTv/Info", TestContext.Current.CancellationToken);
        client.DefaultRequestHeaders.AddAuthHeader(await AuthHelper.CompleteStartupAsync(client));
        var channelsResponse = await client.GetAsync("/Channels", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, liveTvResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, channelsResponse.StatusCode);
    }
}
