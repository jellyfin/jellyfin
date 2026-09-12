using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using SchedulesDirectProvider = Jellyfin.LiveTv.Listings.SchedulesDirect;

namespace Jellyfin.LiveTv.Tests.Listings;

public class SchedulesDirectLineupTests
{
    private const string InvalidUserResponse = "{\"response\":\"INVALID_USER\",\"code\":4003,\"message\":\"Invalid user.\",\"serverID\":\"AWS-SD-web.1\"}";

    private static readonly ListingsProviderInfo _info = new() { Username = "user", Password = "password" };

    [Fact]
    public async Task GetLineups_ValidCredentials_ReturnsLineups()
    {
        var tokenResponse = await CreateSuccessfulLogin();
        using var provider = CreateProvider(tokenResponse, await GetHeadendsResponse());

        var lineups = await provider.GetLineups(_info, "USA", "90210");

        Assert.NotEmpty(lineups);
        Assert.Contains(lineups, i => string.Equals(i.Id, "USA-OTA-90210", StringComparison.Ordinal));
        Assert.Contains(lineups, i => string.Equals(i.Name, "Antenna", StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetLineups_LoginFails_Throws()
    {
        using var provider = CreateProvider(CreateFailedLogin(), await GetHeadendsResponse());

        // An empty lineup list is indistinguishable from "no lineups for this location", so a
        // failed login has to surface as an error instead.
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));
    }

    [Fact]
    public async Task Validate_LoginFails_Throws()
    {
        using var provider = CreateProvider(CreateFailedLogin(), await GetHeadendsResponse());

        await Assert.ThrowsAnyAsync<Exception>(() => provider.Validate(_info, true, false));
    }

    [Fact]
    public async Task Validate_AfterAccountError_RecoversWithoutRestart()
    {
        var login = CreateFailedLogin();
        using var provider = CreateProvider(login, await GetHeadendsResponse());

        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));

        // The account error disables Schedules Direct; saving the provider again is the user
        // correcting their credentials, and that has to recover without a server restart.
        login.Status = HttpStatusCode.OK;
        login.Body = await GetTokenResponse();

        await provider.Validate(_info, true, false);

        Assert.NotEmpty(await provider.GetLineups(_info, "USA", "90210"));
    }

    private static async Task<Response> CreateSuccessfulLogin()
        => new() { Status = HttpStatusCode.OK, Body = await GetTokenResponse() };

    private static Response CreateFailedLogin()
        => new() { Status = HttpStatusCode.BadRequest, Body = InvalidUserResponse };

    private static Task<string> GetTokenResponse()
        => File.ReadAllTextAsync("Test Data/SchedulesDirect/token_live_response.json", TestContext.Current.CancellationToken);

    private static Task<string> GetHeadendsResponse()
        => File.ReadAllTextAsync("Test Data/SchedulesDirect/headends_response.json", TestContext.Current.CancellationToken);

    private static SchedulesDirectProvider CreateProvider(Response login, string headendsResponse)
    {
        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((m, _) =>
            {
                var path = m.RequestUri!.AbsolutePath;
                if (path.EndsWith("/token", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(login.Status)
                    {
                        Content = new StringContent(login.Body)
                    });
                }

                if (path.EndsWith("/headends", StringComparison.Ordinal))
                {
                    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(headendsResponse)
                    });
                }

                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            });

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(messageHandler.Object));

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.SetupGet(x => x.CachePath).Returns(Path.GetTempPath());

        return new SchedulesDirectProvider(
            NullLogger<SchedulesDirectProvider>.Instance,
            httpClientFactory.Object,
            appPaths.Object);
    }

    private sealed class Response
    {
        public HttpStatusCode Status { get; set; }

        public string Body { get; set; } = string.Empty;
    }
}
