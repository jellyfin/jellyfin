using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.LiveTv;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using SchedulesDirectProvider = Jellyfin.LiveTv.Listings.SchedulesDirect;

namespace Jellyfin.LiveTv.Tests.Listings;

public class SchedulesDirectTests : IDisposable
{
    private const string InvalidUserResponse = "{\"response\":\"INVALID_USER\",\"code\":4003,\"message\":\"Invalid user.\",\"serverID\":\"AWS-SD-web.1\"}";
    private const string ImageUrl = "https://json.schedulesdirect.org/20141201/image/assets/p1_b.jpg?token=abc";

    private static readonly ListingsProviderInfo _info = new() { Username = "user", Password = "password" };

    private readonly Mock<ILibraryManager> _libraryManager = new();

    // The daily limit files live under CachePath, so every provider needs its own directory or
    // one test leaves a limit behind for the next.
    private readonly string _cachePath = Path.Combine(Path.GetTempPath(), "jf-sd-" + Guid.NewGuid().ToString("N"));

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

    [Fact]
    public async Task GetLineups_TokenRejectedWithHttp200_DisablesService()
    {
        // The account lockout this guards against arrived as HTTP 200 carrying code 4005.
        var login = new Response
        {
            Status = HttpStatusCode.OK,
            Body = "{\"response\":\"JSON_ACCOUNT_ACCESS_DISABLED\",\"code\":4005,\"serverID\":\"20141201.web\",\"message\":\"Access to account via JSON has been disabled.\"}"
        };
        using var provider = CreateProvider(login, await GetHeadendsResponse());

        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));

        // The account error disables Schedules Direct: a retry must not reach the network again.
        login.Body = await GetTokenResponse();
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));
    }

    [Theory]
    [InlineData(5002, "MAX_IMAGE_DOWNLOADS")]
    [InlineData(5003, "MAX_IMAGE_DOWNLOADS_TRIAL")]
    public async Task ReportImageDownloadFailure_ImageLimitCode_BlocksOnFirstReport(int code, string response)
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        var action = provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ErrorBody(code, response));

        Assert.Equal(ImageDownloadFailureAction.None, action);
        Assert.False(provider.CanDownloadImage(ImageUrl));
    }

    [Fact]
    public async Task ReportImageDownloadFailure_ImageNotFound_DropsUrlWithoutBlockingTheDay()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        var action = provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ErrorBody(5000, "IMAGE_NOT_FOUND"));

        // The spec requires that url is never requested again, but the day is not blocked and
        // every other image stays downloadable.
        Assert.Equal(ImageDownloadFailureAction.RemoveImage, action);
        Assert.False(provider.CanDownloadImage(ImageUrl));
        Assert.True(provider.CanDownloadImage("https://json.schedulesdirect.org/20141201/image/assets/p2_b.jpg?token=abc"));
    }

    [Fact]
    public async Task ReportImageDownloadFailure_InvalidUriLimit_BlocksAndDropsUrl()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        var action = provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ErrorBody(5004, "MAX_IMAGE_INVALID_URI_ERRORS"));

        Assert.Equal(ImageDownloadFailureAction.RemoveImage, action);
        Assert.False(provider.CanDownloadImage(ImageUrl));
    }

    [Theory]
    [InlineData(1004, "TOKEN_MISSING")]
    [InlineData(4006, "TOKEN_EXPIRED")]
    public async Task ReportImageDownloadFailure_TokenCode_DoesNotBlockTheDay(int code, string response)
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        var action = provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.Forbidden, ErrorBody(code, response));

        Assert.Equal(ImageDownloadFailureAction.RemoveImage, action);
        Assert.True(provider.CanDownloadImage(ImageUrl));
    }

    [Fact]
    public async Task ReportImageDownloadFailure_UnreadableBody_FallsBackToFailureStreak()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        for (var i = 0; i < 9; i++)
        {
            provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.Forbidden, "<html>gateway error</html>");
            Assert.True(provider.CanDownloadImage(ImageUrl));
        }

        provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.Forbidden, "<html>gateway error</html>");

        Assert.False(provider.CanDownloadImage(ImageUrl));
    }

    [Fact]
    public async Task ReportImageDownloadSuccess_ResetsUnexplainedFailureStreak()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        for (var i = 0; i < 9; i++)
        {
            provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.Forbidden, null);
        }

        provider.ReportImageDownloadSuccess(ImageUrl);
        provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.Forbidden, null);

        Assert.True(provider.CanDownloadImage(ImageUrl));
    }

    [Fact]
    public async Task ReportImageDownloadFailure_ForeignUrl_IsIgnored()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        var action = provider.ReportImageDownloadFailure("https://example.com/image.jpg", HttpStatusCode.NotFound, ErrorBody(5002, "MAX_IMAGE_DOWNLOADS"));

        Assert.Equal(ImageDownloadFailureAction.None, action);
        Assert.True(provider.CanDownloadImage(ImageUrl));
        Assert.True(provider.CanDownloadImage("https://example.com/image.jpg"));
    }

    [Fact]
    public async Task ConvertImageToLocal_Blocked_DoesNotDownload()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());
        provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ErrorBody(5002, "MAX_IMAGE_DOWNLOADS"));

        var image = await provider.ConvertImageToLocal(new LiveTvProgram(), new ItemImageInfo { Path = ImageUrl, Type = ImageType.Primary }, 0);

        Assert.Null(image);
        _libraryManager.Verify(
            l => l.ConvertImageToLocal(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>(), It.IsAny<int>(), It.IsAny<bool>()),
            Times.Never);
    }

    [Fact]
    public async Task ConvertImageToLocal_ImageNotFound_StopsOfferingTheUri()
    {
        using var provider = CreateProvider(await CreateSuccessfulLogin(), await GetHeadendsResponse());

        // LibraryManager wraps the rejection, which is the only place the SD error body survives.
        _libraryManager.Setup(l => l.ConvertImageToLocal(It.IsAny<BaseItem>(), It.IsAny<ItemImageInfo>(), 0, true))
            .ThrowsAsync(new InvalidOperationException(
                "test",
                new HttpResponseBodyException("test", HttpStatusCode.NotFound, ErrorBody(5000, "IMAGE_NOT_FOUND"))));

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => provider.ConvertImageToLocal(new LiveTvProgram(), new ItemImageInfo { Path = ImageUrl, Type = ImageType.Primary }, 0));

        Assert.False(provider.CanDownloadImage(ImageUrl));
    }

    [Fact]
    public async Task GetProgramsAsync_UnchangedMd5_ServesSecondRefreshFromCache()
    {
        var server = new FakeServer();
        using var provider = CreateProvider(server);

        var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
        Assert.NotEmpty(first);

        var schedulesAfterFirst = server.Counts["/schedules"];
        var programsAfterFirst = server.Counts.GetValueOrDefault("/programs");
        Assert.True(schedulesAfterFirst > 0);
        Assert.True(programsAfterFirst > 0);

        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        // The md5 is unchanged, so only the hash check should have gone to the server.
        Assert.Equal(first.Count(), second.Count());
        Assert.Equal(schedulesAfterFirst, server.Counts["/schedules"]);
        Assert.Equal(programsAfterFirst, server.Counts.GetValueOrDefault("/programs"));
        Assert.Equal(2, server.Counts["/schedules/md5"]);
    }

    [Fact]
    public async Task GetProgramsAsync_ChangedMd5_DownloadsTheScheduleAgain()
    {
        var server = new FakeServer();
        using var provider = CreateProvider(server);

        await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
        var afterFirst = server.Counts["/schedules"];

        server.Md5Salt = "changed";
        await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.True(server.Counts["/schedules"] > afterFirst);
    }

    [Fact]
    public async Task GetProgramsAsync_SystemOffline_ThrowsInsteadOfReportingNoPrograms()
    {
        var server = new FakeServer { SystemStatus = "Offline" };
        using var provider = CreateProvider(server);

        // An empty list means "this channel has no programs", and the guide refresh acts on that
        // by deleting the ones it already has. A failure has to surface as one.
        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        Assert.Equal(0, server.Counts.GetValueOrDefault("/schedules"));
    }

    [Fact]
    public async Task GetProgramsAsync_OfflineCodeWithHttp200_ThrowsInsteadOfReportingNoPrograms()
    {
        // A 200 carrying an error code used to deserialize into an empty status that read as online.
        var server = new FakeServer { StatusCode = 3000 };
        using var provider = CreateProvider(server);

        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        Assert.Equal(0, server.Counts.GetValueOrDefault("/schedules"));
    }

    [Fact]
    public async Task GetProgramsAsync_ImageLimitHit_StillReturnsPrograms()
    {
        // The image limit must not be mistaken for a fetch failure: the listings are still good,
        // only the artwork is unavailable.
        var server = new FakeServer { HasImageArtwork = true };
        using var provider = CreateProvider(server);

        provider.ReportImageDownloadFailure(ImageUrl, HttpStatusCode.NotFound, ErrorBody(5002, "MAX_IMAGE_DOWNLOADS"));
        Assert.False(provider.CanDownloadImage(ImageUrl));

        var programs = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.NotEmpty(programs);
        Assert.All(programs, p => Assert.Null(p.ImageUrl));
        Assert.Equal(0, server.Counts.GetValueOrDefault("/metadata/programs"));
    }

    [Fact]
    public async Task GetProgramsAsync_Md5SaysDateIsOutOfRange_DoesNotRequestTheSchedule()
    {
        // 7020 is what the hash check exists to catch: asking /schedules for the day anyway just
        // earns the same error, which is why the spec points at the md5 endpoint for the range.
        var server = new FakeServer { Md5ErrorCode = 7020 };
        using var provider = CreateProvider(server);

        var programs = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.Empty(programs);
        Assert.Equal(1, server.Counts["/schedules/md5"]);
        Assert.Equal(0, server.Counts.GetValueOrDefault("/schedules"));
    }

    [Fact]
    public async Task GetProgramsAsync_ScheduleQueued_HonoursRetryTime()
    {
        // The queued response is a bare object rather than the usual array, and it carries the
        // earliest time the schedule may be asked for again.
        var server = new FakeServer { ScheduleRetryTime = DateTime.UtcNow.AddMinutes(10) };
        using var provider = CreateProvider(server);

        // A queued schedule is a failure, not an empty channel: reporting no programs would have
        // the guide refresh delete the ones this channel already has.
        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));
        var afterFirst = server.Counts["/schedules"];

        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        Assert.Equal(afterFirst, server.Counts["/schedules"]);
    }

    [Fact]
    public async Task GetProgramsAsync_ScheduleQueuedRetryTimePassed_AsksAgain()
    {
        var server = new FakeServer { ScheduleRetryTime = DateTime.UtcNow.AddSeconds(-1) };
        using var provider = CreateProvider(server);

        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));
        var afterFirst = server.Counts["/schedules"];

        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        Assert.True(server.Counts["/schedules"] > afterFirst);
    }

    [Fact]
    public async Task GetProgramsAsync_CacheHit_StillRefreshesArtwork()
    {
        // Artwork uris are ephemeral and their changes are not reflected in the schedule md5, so
        // the spec forbids storing them; the index has to be requested again on a cache hit.
        var server = new FakeServer { HasImageArtwork = true };
        using var provider = CreateProvider(server);

        var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
        Assert.All(first, p => Assert.Contains("p1_b.jpg", p.ImageUrl, StringComparison.Ordinal));

        var metadataAfterFirst = server.Counts["/metadata/programs"];

        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.Equal(1, server.Counts["/schedules"]);
        Assert.True(server.Counts["/metadata/programs"] > metadataAfterFirst);
        Assert.All(second, p => Assert.Contains("p1_b.jpg", p.ImageUrl, StringComparison.Ordinal));
    }

    [Fact]
    public async Task GetProgramsAsync_AfterImageNotFound_StopsOfferingTheUri()
    {
        var server = new FakeServer { HasImageArtwork = true };
        using var provider = CreateProvider(server);

        var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
        var imageUrl = first.First().ImageUrl;
        Assert.NotNull(imageUrl);

        Assert.Equal(
            ImageDownloadFailureAction.RemoveImage,
            provider.ReportImageDownloadFailure(imageUrl, HttpStatusCode.NotFound, ErrorBody(5000, "IMAGE_NOT_FOUND")));

        // Handing the same dead uri back every refresh is what walks an account into
        // MAX_IMAGE_INVALID_URI_ERRORS, so it must not survive the rejection.
        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.All(second, p => Assert.Null(p.ImageUrl));
    }

    [Fact]
    public async Task GetProgramsAsync_OneDayQueuedNextToCachedDays_ServesThePreviousCopy()
    {
        var server = new FakeServer();
        using var provider = CreateProvider(server);

        var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        // One day changed and is now queued; the others still match the cache. Leaving the queued
        // day out would have the guide refresh delete its programs.
        server.ChangedDate = server.RequestedDates[0];
        server.ScheduleRetryTime = DateTime.UtcNow.AddMinutes(10);
        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.Equal(2, server.Counts["/schedules"]);
        Assert.Equal(first.Select(p => p.Id).Order(), second.Select(p => p.Id).Order());
    }

    [Fact]
    public async Task GetProgramsAsync_ProgramQueued_DoesNotCacheTheIncompleteDay()
    {
        var server = new FakeServer { ProgramErrorCode = 6001 };
        using var provider = CreateProvider(server);

        // With no previous copy to stand in, a queued program fails the channel rather than
        // dropping its airings, which the guide refresh would delete.
        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        // The program finishing generation need not change the day's md5, so the day must be
        // downloaded again rather than served from the cache without it.
        server.ProgramErrorCode = 0;
        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.NotEmpty(second);
        Assert.Equal(2, server.Counts["/programs"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("EP000000020001")]
    public async Task GetProgramsAsync_ProgramQueuedAfterGoodRefresh_KeepsItsAirings(string? queuedProgramId)
    {
        var server = new FakeServer { SecondProgram = true };
        using var provider = CreateProvider(server);

        var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
        Assert.Contains(first, p => p.Id!.Contains("EP000000020001", StringComparison.Ordinal));

        // The day changed and one (or every) program on it is still being generated.
        server.Md5Salt = "changed";
        server.ProgramErrorCode = 6001;
        server.QueuedProgramId = queuedProgramId;
        var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.Equal(first.Select(p => p.Id).Order(), second.Select(p => p.Id).Order());
    }

    [Theory]
    [InlineData(HttpStatusCode.OK, 3000, "SERVICE_OFFLINE")]
    [InlineData(HttpStatusCode.Forbidden, 4009, "TOO_MANY_LOGINS")]
    public async Task GetProgramsAsync_StopCodeOnMd5_DoesNotRequestSchedules(HttpStatusCode status, int code, string response)
    {
        var server = new FakeServer { Md5RejectionBody = ErrorBody(code, response), Md5RejectionStatus = status };
        using var provider = CreateProvider(server);

        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));

        Assert.Equal(0, server.Counts.GetValueOrDefault("/schedules"));
    }

    [Theory]
    [InlineData(1004, "TOKEN_MISSING")]
    [InlineData(1010, "INVALID_TOKEN")]
    public async Task GetProgramsAsync_TokenRejected_LogsInAgain(int code, string response)
    {
        var server = new FakeServer { Md5RejectionBody = ErrorBody(code, response) };
        using var provider = CreateProvider(server);

        var programs = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);

        Assert.NotEmpty(programs);
        Assert.Equal(2, server.Counts["/token"]);
    }

    [Fact]
    public async Task GetProgramsAsync_ServiceOffline_DoesNotLogInAgainDuringTheOfflineHour()
    {
        var server = new FakeServer { StatusCode = 3000 };
        using var provider = CreateProvider(server);

        for (var i = 0; i < 3; i++)
        {
            await Assert.ThrowsAnyAsync<Exception>(
                () => provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken));
        }

        // MAX_LOGIN_ATTEMPTS counts logins, so each channel of a refresh must not log in again.
        Assert.Equal(1, server.Counts["/token"]);
    }

    [Fact]
    public async Task GetProgramsAsync_AfterImageNotFoundAndRestart_StopsOfferingTheUri()
    {
        var server = new FakeServer { HasImageArtwork = true };

        using (var provider = CreateProvider(server))
        {
            var first = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
            provider.ReportImageDownloadFailure(first.First().ImageUrl!, HttpStatusCode.NotFound, ErrorBody(5000, "IMAGE_NOT_FOUND"));
        }

        // A second provider over the same cache directory stands in for a restart.
        using (var provider = CreateProvider(server))
        {
            var second = await provider.GetProgramsAsync(_info, "20454", DateTime.UtcNow, DateTime.UtcNow.AddDays(1), TestContext.Current.CancellationToken);
            Assert.All(second, p => Assert.Null(p.ImageUrl));
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden, 4009, "TOO_MANY_LOGINS")]
    [InlineData(HttpStatusCode.OK, 4009, "TOO_MANY_LOGINS")]
    [InlineData(HttpStatusCode.Forbidden, 4010, "TOO_MANY_UNIQUE_IPS")]
    public async Task GetLineups_AccountLimitReached_StopsLoggingIn(HttpStatusCode status, int code, string response)
    {
        var login = new Response
        {
            Status = status,
            Body = ErrorBody(code, response)
        };
        using var provider = CreateProvider(login, await GetHeadendsResponse());

        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));

        // These codes count logins, so continuing to ask for a token is what earned them: a
        // retry must not go back to /token even once the credentials would work.
        login.Status = HttpStatusCode.OK;
        login.Body = await GetTokenResponse();
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetLineups(_info, "USA", "90210"));
    }

    [Fact]
    public async Task GetChannels_SystemOffline_ThrowsInsteadOfReportingNoChannels()
    {
        var server = new FakeServer { SystemStatus = "Offline" };
        using var provider = CreateProvider(server);

        // No channels means no channel is mapped, which the guide refresh reads as "no programs".
        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.GetChannels(new ListingsProviderInfo { Username = "user", Password = "password", ListingsId = "USA-OTA-90210" }, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetChannels_LoginBackedOff_ThrowsInsteadOfReportingNoChannels()
    {
        var login = new Response { Status = HttpStatusCode.Forbidden, Body = ErrorBody(4009, "TOO_MANY_LOGINS") };
        using var provider = CreateProvider(login, await GetHeadendsResponse());
        var info = new ListingsProviderInfo { Username = "user", Password = "password", ListingsId = "USA-OTA-90210" };

        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetChannels(info, TestContext.Current.CancellationToken));

        // The second call never reaches /token, so this is the null-token path.
        await Assert.ThrowsAnyAsync<Exception>(() => provider.GetChannels(info, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetChannels_UnchangedModifiedDate_ServesTheLineupFromCache()
    {
        var server = new FakeServer();
        using var provider = CreateProvider(server);
        var info = new ListingsProviderInfo { Username = "user", Password = "password", ListingsId = "USA-OTA-90210" };

        var first = await provider.GetChannels(info, TestContext.Current.CancellationToken);
        Assert.NotEmpty(first);
        Assert.Equal(1, server.Counts["/lineups/USA-OTA-90210"]);

        var second = await provider.GetChannels(info, TestContext.Current.CancellationToken);

        Assert.Equal(first.Count, second.Count);
        Assert.Equal(1, server.Counts["/lineups/USA-OTA-90210"]);
    }

    [Fact]
    public async Task GetChannels_NewerModifiedDate_DownloadsTheLineupAgain()
    {
        var server = new FakeServer();
        var info = new ListingsProviderInfo { Username = "user", Password = "password", ListingsId = "USA-OTA-90210" };

        using (var provider = CreateProvider(server))
        {
            await provider.GetChannels(info, TestContext.Current.CancellationToken);
        }

        server.LineupModified = server.LineupModified.AddDays(1);

        // A second provider over the same cache directory stands in for a restart, which is also
        // what makes the status re-read rather than coming from its short-lived cache.
        using (var provider = CreateProvider(server))
        {
            await provider.GetChannels(info, TestContext.Current.CancellationToken);
        }

        Assert.Equal(2, server.Counts["/lineups/USA-OTA-90210"]);
    }

    [Fact]
    public async Task Validate_LineupCannotBeAdded_Throws()
    {
        var server = new FakeServer { LineupPutStatus = HttpStatusCode.BadRequest };
        using var provider = CreateProvider(server);

        // Silently reporting success leaves the user with a provider that has no lineup.
        await Assert.ThrowsAnyAsync<Exception>(
            () => provider.Validate(new ListingsProviderInfo { Username = "user", Password = "password", ListingsId = "USA-OTA-90210" }, true, true));
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && Directory.Exists(_cachePath))
        {
            Directory.Delete(_cachePath, true);
        }
    }

    // Schedules Direct answers an exhausted image quota with HTTP 200 and a body like this, which
    // ProviderManager surfaces as a synthetic 404 because it is not an image content type.
    private static string ErrorBody(int code, string response)
        => string.Format(
            CultureInfo.InvariantCulture,
            "{{\"response\":\"{0}\",\"code\":{1},\"serverID\":\"20141201.debug\",\"message\":\"test\",\"datetime\":\"2026-09-14T00:00:00Z\"}}",
            response,
            code);

    private static async Task<Response> CreateSuccessfulLogin()
        => new() { Status = HttpStatusCode.OK, Body = await GetTokenResponse() };

    private static Response CreateFailedLogin()
        => new() { Status = HttpStatusCode.BadRequest, Body = InvalidUserResponse };

    private static Task<string> GetTokenResponse()
        => File.ReadAllTextAsync("Test Data/SchedulesDirect/token_live_response.json", TestContext.Current.CancellationToken);

    private static Task<string> GetHeadendsResponse()
        => File.ReadAllTextAsync("Test Data/SchedulesDirect/headends_response.json", TestContext.Current.CancellationToken);

    private SchedulesDirectProvider CreateProvider(Response login, string headendsResponse)
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
        appPaths.SetupGet(x => x.CachePath).Returns(_cachePath);

        return new SchedulesDirectProvider(
            NullLogger<SchedulesDirectProvider>.Instance,
            httpClientFactory.Object,
            appPaths.Object,
            _libraryManager.Object);
    }

    private SchedulesDirectProvider CreateProvider(FakeServer server)
    {
        var messageHandler = new Mock<HttpMessageHandler>();
        messageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .Returns<HttpRequestMessage, CancellationToken>((m, ct) => server.Respond(m, ct));

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(messageHandler.Object));

        var appPaths = new Mock<IApplicationPaths>();
        appPaths.SetupGet(x => x.CachePath).Returns(_cachePath);

        return new SchedulesDirectProvider(
            NullLogger<SchedulesDirectProvider>.Instance,
            httpClientFactory.Object,
            appPaths.Object,
            _libraryManager.Object);
    }

    /// <summary>
    /// A minimal stand-in for the Schedules Direct API that counts what was asked of it.
    /// </summary>
    private sealed class FakeServer
    {
        public Dictionary<string, int> Counts { get; } = new(StringComparer.Ordinal);

        public string SystemStatus { get; set; } = "Online";

        public int StatusCode { get; set; }

        public string Md5Salt { get; set; } = "base";

        public HttpStatusCode LineupPutStatus { get; set; } = HttpStatusCode.OK;

        /// <summary>
        /// Gets or sets a per-day error code for the hash endpoint, as SCHEDULE_RANGE_EXCEEDED is
        /// reported. Zero means every day is available.
        /// </summary>
        public int Md5ErrorCode { get; set; }

        /// <summary>
        /// Gets or sets the retryTime of a SCHEDULE_QUEUED response, which the spec documents as a
        /// bare object rather than the usual array.
        /// </summary>
        public DateTime? ScheduleRetryTime { get; set; }

        public bool HasImageArtwork { get; set; }

        /// <summary>
        /// Gets or sets a date whose schedule md5 differs from the one first reported.
        /// </summary>
        public string? ChangedDate { get; set; }

        public List<string> RequestedDates { get; private set; } = [];

        /// <summary>
        /// Gets or sets a per-program error code for the programs endpoint, such as PROGRAMID_QUEUED.
        /// </summary>
        public int ProgramErrorCode { get; set; }

        /// <summary>
        /// Gets or sets a body the hash endpoint rejects the next request's token with, as HTTP 403.
        /// </summary>
        public string? Md5RejectionBody { get; set; }

        public HttpStatusCode Md5RejectionStatus { get; set; } = HttpStatusCode.Forbidden;

        /// <summary>
        /// Gets or sets a value indicating whether each day airs a second program.
        /// </summary>
        public bool SecondProgram { get; set; }

        /// <summary>
        /// Gets or sets the only program <see cref="ProgramErrorCode"/> applies to; null means all of them.
        /// </summary>
        public string? QueuedProgramId { get; set; }

        public string ImageUri { get; set; } = "assets/p1_b.jpg";

        public DateTime LineupModified { get; set; } = new(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc);

        public async Task<HttpResponseMessage> Respond(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var path = message.RequestUri!.AbsolutePath.Replace("/20141201", string.Empty, StringComparison.Ordinal).TrimEnd('/');
            Counts[path] = Counts.GetValueOrDefault(path) + 1;

            string body;
            switch (path)
            {
                case "/token":
                    body = "{\"code\":0,\"message\":\"OK\",\"serverID\":\"test\",\"token\":\"f3fca79989cafe7dead71beefedc812b\"}";
                    break;

                case "/status":
                    body = "{\"code\":" + StatusCode.ToString(CultureInfo.InvariantCulture) + ",\"account\":{\"maxLineups\":4},\"lineups\":[{\"lineup\":\"USA-OTA-90210\",\"modified\":\""
                        + LineupModified.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)
                        + "\"}],\"systemStatus\":[{\"status\":\"" + SystemStatus + "\",\"message\":\"test\"}]}";
                    break;

                case "/schedules/md5":
                    if (Md5RejectionBody is not null)
                    {
                        var rejection = Md5RejectionBody;
                        Md5RejectionBody = null;
                        return new HttpResponseMessage(Md5RejectionStatus) { Content = new StringContent(rejection) };
                    }

                    RequestedDates = await ReadDates(message, cancellationToken).ConfigureAwait(false);
                    body = BuildMd5Response(RequestedDates);
                    break;

                case "/schedules":
                    body = BuildSchedulesResponse(await ReadDates(message, cancellationToken).ConfigureAwait(false));
                    break;

                case "/programs":
                    var ids = JsonSerializer.Deserialize<List<string>>(await message.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false))!;
                    body = "[" + string.Join(',', ids.Select(BuildProgramEntry)) + "]";
                    break;

                case "/metadata/programs":
                    body = HasImageArtwork
                        ? "[{\"programID\":\"EP000000010001\",\"data\":[{\"width\":\"120\",\"height\":\"180\",\"uri\":\""
                            + ImageUri + "\",\"aspect\":\"2x3\",\"category\":\"Iconic\",\"text\":\"yes\",\"tier\":\"Series\"}]}]"
                        : "[]";
                    break;

                case "/lineups/USA-OTA-90210":
                    if (message.Method == HttpMethod.Get)
                    {
                        body = "{\"map\":[{\"stationID\":\"20454\",\"channel\":\"5\"}],\"stations\":[{\"stationID\":\"20454\",\"name\":\"Test\",\"callsign\":\"TST\"}]}";
                        break;
                    }

                    return new HttpResponseMessage(LineupPutStatus)
                    {
                        Content = new StringContent(LineupPutStatus == HttpStatusCode.OK
                            ? "{\"code\":0,\"message\":\"OK\"}"
                            : "{\"response\":\"MAX_LINEUPS\",\"code\":4101,\"message\":\"Exceeded number of lineups.\"}")
                    };

                case "/lineups":
                    body = "{\"code\":0,\"lineups\":[]}";
                    break;

                default:
                    return new HttpResponseMessage(HttpStatusCode.NotFound) { Content = new StringContent("{}") };
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body) };
        }

        private static async Task<List<string>> ReadDates(HttpRequestMessage message, CancellationToken cancellationToken)
        {
            var requestBody = await message.Content!.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            using var doc = JsonDocument.Parse(requestBody);

            var dates = new List<string>();
            foreach (var station in doc.RootElement.EnumerateArray())
            {
                foreach (var date in station.GetProperty("date").EnumerateArray())
                {
                    dates.Add(date.GetString()!);
                }
            }

            return dates;
        }

        private string BuildMd5Response(List<string> dates)
        {
            var days = dates.ConvertAll(d => Md5ErrorCode == 0
                ? "\"" + d + "\":{\"code\":0,\"message\":\"OK\",\"lastModified\":\"2026-09-14T00:00:00Z\",\"md5\":\"" + GetMd5(d) + "\"}"
                : "\"" + d + "\":{\"code\":" + Md5ErrorCode.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"Date requested not within range.\"}");

            return "{\"20454\":{" + string.Join(',', days) + "}}";
        }

        private string GetMd5(string date)
            => (string.Equals(date, ChangedDate, StringComparison.Ordinal) ? "changed" : Md5Salt) + date;

        private string BuildProgramEntry(string id)
            => ProgramErrorCode != 0 && (QueuedProgramId is null || string.Equals(id, QueuedProgramId, StringComparison.Ordinal))
                ? "{\"programID\":\"" + id + "\",\"code\":" + ProgramErrorCode.ToString(CultureInfo.InvariantCulture) + ",\"message\":\"test\"}"
                : "{\"programID\":\"" + id + "\",\"titles\":[{\"title120\":\"Test\"}],\"hasImageArtwork\":"
                    + (HasImageArtwork ? "true" : "false") + "}";

        private string BuildSchedulesResponse(List<string> dates)
        {
            if (ScheduleRetryTime is { } retryTime)
            {
                return "{\"response\":\"SCHEDULE_QUEUED\",\"code\":7100,\"serverID\":\"test\",\"message\":\"Queued.\",\"stationID\":\"20454\",\"retryTime\":\""
                    + retryTime.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture) + "\"}";
            }

            var days = dates.ConvertAll(d =>
                "{\"stationID\":\"20454\",\"programs\":[{\"programID\":\"EP000000010001\",\"airDateTime\":\"" + d
                + "T12:00:00Z\",\"duration\":1800,\"md5\":\"x\"}"
                + (SecondProgram ? ",{\"programID\":\"EP000000020001\",\"airDateTime\":\"" + d + "T14:00:00Z\",\"duration\":1800,\"md5\":\"y\"}" : string.Empty)
                + "],\"metadata\":{\"modified\":\"2026-09-14T00:00:00Z\",\"md5\":\""
                + GetMd5(d) + "\",\"startDate\":\"" + d + "\"}}");

            return "[" + string.Join(',', days) + "]";
        }
    }

    private sealed class Response
    {
        public HttpStatusCode Status { get; set; }

        public string Body { get; set; } = string.Empty;
    }
}
