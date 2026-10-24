using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Extensions.Json;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Querying;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Jellyfin.Server.Integration.Tests;

/// <summary>
/// Covers Next Up selecting the same episodes regardless of whether user data is included in the
/// response, which only controls the shape of the payload, not which episodes are picked.
/// </summary>
public sealed class NextUpUserDataTests : IClassFixture<JellyfinApplicationFactory>
{
    private const string EpisodeType = "MediaBrowser.Controller.Entities.TV.Episode";
    private const string SeriesType = "MediaBrowser.Controller.Entities.TV.Series";

    private static string? _accessToken;

    private readonly JellyfinApplicationFactory _factory;

    public NextUpUserDataTests(JellyfinApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ResumableEpisode_ExcludedRegardlessOfEnableUserData()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        var user = await AuthHelper.GetUserDtoAsync(client);

        var seriesId = Guid.NewGuid();
        var seriesKey = seriesId.ToString("N");
        var played = Guid.NewGuid();
        var resumable = Guid.NewGuid();

        await using (var context = await CreateDbContextAsync())
        {
            context.BaseItems.Add(CreateSeries(seriesId, "Resume Series", seriesKey));
            context.BaseItems.Add(CreateEpisode(played, seriesKey, 1, 1));
            context.BaseItems.Add(CreateEpisode(resumable, seriesKey, 1, 2));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.UserData.Add(CreateUserData(played, user.Id, played: true));
            context.UserData.Add(CreateUserData(resumable, user.Id, played: false, playbackPositionTicks: 10_000_000));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var withUserData = await GetNextUpAsync(client, user.Id, seriesId, enableUserData: true);
        var withoutUserData = await GetNextUpAsync(client, user.Id, seriesId, enableUserData: false);

        // The only unplayed episode after the played one is resumable, and resumable episodes are
        // excluded, so neither request offers it
        Assert.Empty(withUserData.Items);
        Assert.Empty(withoutUserData.Items);
    }

    [Fact]
    public async Task PlayedSpecial_ExcludedRegardlessOfEnableUserData()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        var user = await AuthHelper.GetUserDtoAsync(client);

        var configuration = _factory.Services.GetRequiredService<IServerConfigurationManager>().Configuration;
        var displaySpecialsWithinSeasons = configuration.DisplaySpecialsWithinSeasons;
        configuration.DisplaySpecialsWithinSeasons = true;

        try
        {
            await AssertPlayedSpecialIsNotOfferedAsync(client, user.Id);
        }
        finally
        {
            configuration.DisplaySpecialsWithinSeasons = displaySpecialsWithinSeasons;
        }
    }

    private async Task AssertPlayedSpecialIsNotOfferedAsync(HttpClient client, Guid userId)
    {
        var seriesId = Guid.NewGuid();
        var seriesKey = seriesId.ToString("N");
        var played = Guid.NewGuid();
        var special = Guid.NewGuid();

        await using (var context = await CreateDbContextAsync())
        {
            context.BaseItems.Add(CreateSeries(seriesId, "Specials Series", seriesKey));
            context.BaseItems.Add(CreateEpisode(played, seriesKey, 1, 1));
            var specialEntity = CreateEpisode(special, seriesKey, 0, 1);
            specialEntity.Name = "Special 1";

            // The air position lives in the serialized data blob, and this one aired after the only
            // season, so the special is a candidate for the next episode
            specialEntity.Data = "{\"AirsAfterSeasonNumber\":1}";
            context.BaseItems.Add(specialEntity);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.UserData.Add(CreateUserData(played, userId, played: true));
            context.UserData.Add(CreateUserData(special, userId, played: true));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var withUserData = await GetNextUpAsync(client, userId, seriesId, enableUserData: true);
        var withoutUserData = await GetNextUpAsync(client, userId, seriesId, enableUserData: false);

        // Every regular episode is played and the only other candidate is a played special, so
        // neither request offers anything
        Assert.Empty(withUserData.Items);
        Assert.Empty(withoutUserData.Items);
    }

    [Fact]
    public async Task UnplayedEpisode_ReturnedRegardlessOfEnableUserData()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.AddAuthHeader(_accessToken ??= await AuthHelper.CompleteStartupAsync(client));
        var user = await AuthHelper.GetUserDtoAsync(client);

        var seriesId = Guid.NewGuid();
        var seriesKey = seriesId.ToString("N");
        var played = Guid.NewGuid();
        var next = Guid.NewGuid();

        await using (var context = await CreateDbContextAsync())
        {
            context.BaseItems.Add(CreateSeries(seriesId, "Plain Series", seriesKey));
            context.BaseItems.Add(CreateEpisode(played, seriesKey, 1, 1));
            context.BaseItems.Add(CreateEpisode(next, seriesKey, 1, 2));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            context.UserData.Add(CreateUserData(played, user.Id, played: true));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var withUserData = await GetNextUpAsync(client, user.Id, seriesId, enableUserData: true);
        var withoutUserData = await GetNextUpAsync(client, user.Id, seriesId, enableUserData: false);

        Assert.Equal(next, Assert.Single(withUserData.Items).Id);
        Assert.Equal(next, Assert.Single(withoutUserData.Items).Id);
    }

    private static async Task<QueryResult<BaseItemDto>> GetNextUpAsync(
        HttpClient client,
        Guid userId,
        Guid seriesId,
        bool enableUserData)
    {
        using var response = await client.GetAsync(
            $"/Shows/NextUp?userId={userId}&seriesId={seriesId}&enableResumable=false&enableUserData={enableUserData}",
            TestContext.Current.CancellationToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content.ReadFromJsonAsync<QueryResult<BaseItemDto>>(
            JsonDefaults.Options,
            TestContext.Current.CancellationToken);
        Assert.NotNull(result);

        return result;
    }

    private Task<JellyfinDbContext> CreateDbContextAsync()
        => _factory.Services.GetRequiredService<IDbContextFactory<JellyfinDbContext>>()
            .CreateDbContextAsync(TestContext.Current.CancellationToken);

    private static BaseItemEntity CreateSeries(Guid id, string name, string presentationUniqueKey)
        => new()
        {
            Id = id,
            Type = SeriesType,
            Name = name,
            PresentationUniqueKey = presentationUniqueKey,
            IsFolder = true
        };

    private static BaseItemEntity CreateEpisode(Guid id, string seriesKey, int seasonNumber, int indexNumber)
        => new()
        {
            Id = id,
            Type = EpisodeType,
            Name = $"Episode {indexNumber}",
            SeriesPresentationUniqueKey = seriesKey,
            ParentIndexNumber = seasonNumber,
            IndexNumber = indexNumber,
            PresentationUniqueKey = id.ToString("N")
        };

    private static UserData CreateUserData(
        Guid itemId,
        Guid userId,
        bool played,
        long playbackPositionTicks = 0)
        => new()
        {
            ItemId = itemId,
            UserId = userId,
            CustomDataKey = itemId.ToString("N"),
            Played = played,
            PlaybackPositionTicks = playbackPositionTicks,
            Item = null!,
            User = null!
        };
}
