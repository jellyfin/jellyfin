using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library.Recommendations;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Library.Recommendations;

/// <summary>
/// Unit tests for <see cref="RecommendationsService"/>.
/// </summary>
public sealed class RecommendationsServiceTests
{
    private static (RecommendationsService Svc,
                    Mock<ILibraryManager> Lib,
                    Mock<IUserDataManager> UserData,
                    Mock<IPeopleRepository> People,
                    Mock<IDtoService> Dto) MakeService()
    {
        var lib = new Mock<ILibraryManager>();
        var userData = new Mock<IUserDataManager>();
        var people = new Mock<IPeopleRepository>();
        var dto = new Mock<IDtoService>();
        var userMgr = new Mock<IUserManager>();
        var logger = Mock.Of<ILogger<RecommendationsService>>();

        lib.Setup(l => l.GetItemList(It.IsAny<InternalItemsQuery>())).Returns(new List<BaseItem>());
        people.Setup(p => p.GetPeople(It.IsAny<InternalPeopleQuery>()))
              .Returns(new MediaBrowser.Model.Querying.QueryResult<PersonInfo> { Items = Array.Empty<PersonInfo>() });
        userMgr.Setup(u => u.GetUserById(It.IsAny<Guid>())).Returns(new User("u", "default", "default"));

        var svc = new RecommendationsService(
            lib.Object,
            userData.Object,
            people.Object,
            dto.Object,
            userMgr.Object,
            logger);
        return (svc, lib, userData, people, dto);
    }

    /// <summary>
    /// A cold-start user (no watch/favorite history) should receive an empty recommendations list.
    /// </summary>
    [Fact]
    public async Task GetRecommendationsAsync_ColdStart_ReturnsEmpty()
    {
        var (svc, _, _, _, _) = MakeService();
        var req = new RecommendationRequest(Guid.NewGuid(), BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        var result = await svc.GetRecommendationsAsync(req, CancellationToken.None);

        Assert.Empty(result);
    }

    /// <summary>
    /// The second call with the same user+kind should reuse the cached profile (only one history query).
    /// </summary>
    [Fact]
    public async Task GetRecommendationsAsync_CachesProfileAcrossCalls()
    {
        var (svc, lib, _, _, _) = MakeService();
        var req = new RecommendationRequest(Guid.NewGuid(), BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);
        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var historyQueryCount = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Count(q => q.IsPlayed == true && q.Limit == 500);
        Assert.Equal(1, historyQueryCount);
    }

    /// <summary>
    /// A TogglePlayed event for the same user should evict the cached profile so the next
    /// call rebuilds it (two distinct history queries expected).
    /// </summary>
    [Fact]
    public async Task UserDataSaved_PlayedChange_InvalidatesCache()
    {
        var (svc, lib, userData, _, _) = MakeService();
        var userId = Guid.NewGuid();
        var req = new RecommendationRequest(userId, BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        userData.Raise(u => u.UserDataSaved += null, new UserDataSaveEventArgs
        {
            UserId = userId,
            Item = new Movie { Id = Guid.NewGuid() },
            UserData = new UserItemData { Key = "k" },
            SaveReason = UserDataSaveReason.TogglePlayed
        });

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var historyQueryCount = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Count(q => q.IsPlayed == true && q.Limit == 500);
        Assert.Equal(2, historyQueryCount);
    }

    /// <summary>
    /// A PlaybackProgress event must NOT evict the cache — only one history query expected.
    /// </summary>
    [Fact]
    public async Task UserDataSaved_PlaybackProgressChange_DoesNotInvalidateCache()
    {
        var (svc, lib, userData, _, _) = MakeService();
        var userId = Guid.NewGuid();
        var req = new RecommendationRequest(userId, BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        userData.Raise(u => u.UserDataSaved += null, new UserDataSaveEventArgs
        {
            UserId = userId,
            Item = new Movie { Id = Guid.NewGuid() },
            UserData = new UserItemData { Key = "k" },
            SaveReason = UserDataSaveReason.PlaybackProgress
        });

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var historyQueryCount = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Count(q => q.IsPlayed == true && q.Limit == 500);
        Assert.Equal(1, historyQueryCount);
    }

    /// <summary>
    /// TryGetRecommendableKind should accept Movie and Series singletons, and reject
    /// mixed, unsupported, or empty type lists.
    /// </summary>
    [Fact]
    public void TryGetRecommendableKind_StaticHelperContractsHold()
    {
        Assert.True(RecommendationsService.TryGetRecommendableKind(new[] { BaseItemKind.Movie }, Array.Empty<MediaType>(), out var k1));
        Assert.Equal(BaseItemKind.Movie, k1);
        Assert.True(RecommendationsService.TryGetRecommendableKind(new[] { BaseItemKind.Series }, Array.Empty<MediaType>(), out var k2));
        Assert.Equal(BaseItemKind.Series, k2);
        Assert.False(RecommendationsService.TryGetRecommendableKind(new[] { BaseItemKind.Movie, BaseItemKind.Series }, Array.Empty<MediaType>(), out _));
        Assert.False(RecommendationsService.TryGetRecommendableKind(new[] { BaseItemKind.Photo }, Array.Empty<MediaType>(), out _));
        Assert.False(RecommendationsService.TryGetRecommendableKind(Array.Empty<BaseItemKind>(), Array.Empty<MediaType>(), out _));
    }

    /// <summary>
    /// When the user has recently played seeds, GetRecommendationsAsync should emit
    /// a SimilarToRecentlyPlayed category with matching candidates.
    /// </summary>
    [Fact]
    public async Task GetRecommendationsAsync_EmitsCategoryForRecentlyPlayedSeed()
    {
        var (svc, lib, userData, people, dto) = MakeService();
        var user = new User("u", "default", "default");
        var userId = user.Id;
        var seedMovie = new Movie
        {
            Id = Guid.NewGuid(),
            Name = "Inception",
            Genres = new[] { "Sci-Fi" }
        };
        var candidate1 = new Movie { Id = Guid.NewGuid(), Name = "Interstellar", Genres = new[] { "Sci-Fi" } };
        var candidate2 = new Movie { Id = Guid.NewGuid(), Name = "Memento", Genres = new[] { "Thriller" } };
        var candidate3 = new Movie { Id = Guid.NewGuid(), Name = "Tenet", Genres = new[] { "Sci-Fi" } };
        var candidate4 = new Movie { Id = Guid.NewGuid(), Name = "Prestige", Genres = new[] { "Sci-Fi" } };

        // History fetch (IsPlayed = true, Limit = 500) returns the seed
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 500)))
           .Returns(new List<BaseItem> { seedMovie });
        // Favorites fetch (IsFavoriteOrLiked = true) returns empty
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavoriteOrLiked == true)))
           .Returns(new List<BaseItem>());
        // Seed selection (the recent-6 query) — same as history but Limit = 6
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 6)))
           .Returns(new List<BaseItem> { seedMovie });
        // Candidate pool query (has Genres = seed.Genres)
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q =>
                q.Genres != null && q.Genres.Count > 0 && q.Genres.Contains("Sci-Fi") && q.IsPlayed != true)))
           .Returns(new List<BaseItem> { candidate1, candidate2, candidate3, candidate4 });
        userData.Setup(u => u.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
                .Returns(new UserItemData { Key = "k", IsFavorite = false, Likes = false, Played = true });
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem, bool>((items, _, _, _, _) => items.Select(i => new BaseItemDto { Id = i.Id, Name = i.Name }).ToList());

        var req = new RecommendationRequest(userId, BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 4, new DtoOptions());

        var result = await svc.GetRecommendationsAsync(req, CancellationToken.None);

        Assert.NotEmpty(result);
        var firstCategory = result.First();
        Assert.Equal(MediaBrowser.Model.Dto.RecommendationType.SimilarToRecentlyPlayed, firstCategory.RecommendationType);
        Assert.Equal("Inception", firstCategory.BaselineItemName);
        Assert.Contains(firstCategory.Items, i => i.Name == "Interstellar");
        Assert.DoesNotContain(firstCategory.Items, i => i.Name == "Inception");
    }

    /// <summary>
    /// When candidate pool is too small (fewer than itemLimit/2), the category is skipped
    /// and GetRecommendationsAsync returns an empty list.
    /// </summary>
    [Fact]
    public async Task GetRecommendationsAsync_SkipsCategoryWhenTooFewResults()
    {
        var (svc, lib, userData, people, dto) = MakeService();
        var userId = Guid.NewGuid();
        var seedMovie = new Movie { Id = Guid.NewGuid(), Name = "Solo", Genres = new[] { "Obscure" } };
        var weakCandidate = new Movie { Id = Guid.NewGuid(), Name = "OnlyMatch", Genres = new[] { "Obscure" } };

        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 500)))
           .Returns(new List<BaseItem> { seedMovie });
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavoriteOrLiked == true)))
           .Returns(new List<BaseItem>());
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 6)))
           .Returns(new List<BaseItem> { seedMovie });
        // Only one candidate matches — itemLimit/2 = 4, so 1 < 4 → category skipped
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.Genres != null && q.Genres.Count > 0 && q.IsPlayed != true)))
           .Returns(new List<BaseItem> { weakCandidate });
        userData.Setup(u => u.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
                .Returns(new UserItemData { Key = "k", Played = true });
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns(new List<BaseItemDto>());

        var req = new RecommendationRequest(userId, BaseItemKind.Movie, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        var result = await svc.GetRecommendationsAsync(req, CancellationToken.None);

        Assert.Empty(result);
    }

    /// <summary>
    /// A cold-start user (no watch history) should cause GetRankedItemsAsync to return null
    /// so the caller can fall back to its existing behavior.
    /// </summary>
    [Fact]
    public async Task GetRankedItemsAsync_ColdStart_ReturnsNullSoCallerCanFallBack()
    {
        var (svc, _, _, _, _) = MakeService();
        var result = await svc.GetRankedItemsAsync(
            Guid.NewGuid(),
            BaseItemKind.Movie,
            parentId: null,
            startIndex: null,
            limit: 10,
            enableTotalRecordCount: false,
            new DtoOptions(),
            CancellationToken.None);
        Assert.Null(result);
    }

    /// <summary>
    /// When a taste profile exists, GetRankedItemsAsync should return items ordered
    /// highest-scoring first based on genre affinity.
    /// </summary>
    [Fact]
    public async Task GetRankedItemsAsync_WithProfile_ReturnsHighestScoredFirst()
    {
        var (svc, lib, userData, people, dto) = MakeService();
        var userId = Guid.NewGuid();
        var watched = new Movie
        {
            Id = Guid.NewGuid(),
            Genres = new[] { "Sci-Fi" }
        };
        var candidateHigh = new Movie { Id = Guid.NewGuid(), Name = "HighMatch", Genres = new[] { "Sci-Fi" } };
        var candidateLow = new Movie { Id = Guid.NewGuid(), Name = "LowMatch", Genres = new[] { "Comedy" } };

        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 500)))
           .Returns(new List<BaseItem> { watched });
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavoriteOrLiked == true && q.Limit == 250)))
           .Returns(new List<BaseItem>());
        // Candidate pool query for the ranked-list path: IsPlayed = false, no Genres filter
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IncludeItemTypes != null && q.IncludeItemTypes.Length == 1 && q.IncludeItemTypes[0] == BaseItemKind.Movie && q.IsPlayed == false && q.Genres.Count == 0)))
           .Returns(new List<BaseItem> { candidateLow, candidateHigh });
        userData.Setup(u => u.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
                .Returns(new UserItemData { Key = "k", Played = true });
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem, bool>((items, _, _, _, _) => items.Select(i => new BaseItemDto { Id = i.Id, Name = i.Name }).ToList());

        var result = await svc.GetRankedItemsAsync(
            userId,
            BaseItemKind.Movie,
            parentId: null,
            startIndex: null,
            limit: 10,
            enableTotalRecordCount: false,
            new DtoOptions(),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("HighMatch", result!.Items[0].Name);
    }

    /// <summary>
    /// Playing a TV episode must invalidate the cached Series profile (episodes/seasons roll up
    /// to the Series recommendable kind), so the next call rebuilds it.
    /// </summary>
    [Fact]
    public async Task UserDataSaved_EpisodePlayed_InvalidatesSeriesProfile()
    {
        var (svc, lib, userData, _, _) = MakeService();
        var userId = Guid.NewGuid();
        var req = new RecommendationRequest(userId, BaseItemKind.Series, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        userData.Raise(u => u.UserDataSaved += null, new UserDataSaveEventArgs
        {
            UserId = userId,
            Item = new Episode { Id = Guid.NewGuid() },
            UserData = new UserItemData { Key = "k" },
            SaveReason = UserDataSaveReason.PlaybackFinished
        });

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var historyQueryCount = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Count(q => q.IsPlayed == true && q.Limit == 500);
        Assert.Equal(2, historyQueryCount);
    }

    /// <summary>
    /// Playing a Movie must NOT invalidate a cached Series profile — the two kinds are independent.
    /// </summary>
    [Fact]
    public async Task UserDataSaved_MoviePlayed_DoesNotInvalidateSeriesProfile()
    {
        var (svc, lib, userData, _, _) = MakeService();
        var userId = Guid.NewGuid();
        var req = new RecommendationRequest(userId, BaseItemKind.Series, ParentId: null, CategoryLimit: 5, ItemLimit: 8, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        userData.Raise(u => u.UserDataSaved += null, new UserDataSaveEventArgs
        {
            UserId = userId,
            Item = new Movie { Id = Guid.NewGuid() },
            UserData = new UserItemData { Key = "k" },
            SaveReason = UserDataSaveReason.TogglePlayed
        });

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var historyQueryCount = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Count(q => q.IsPlayed == true && q.Limit == 500);
        Assert.Equal(1, historyQueryCount);
    }

    /// <summary>
    /// A pathologically large itemLimit must be clamped before the *4 candidate-pool multiplication
    /// so it never overflows int into a negative DB Limit.
    /// </summary>
    [Fact]
    public async Task GetRecommendationsAsync_HugeItemLimit_DoesNotProduceNegativeQueryLimit()
    {
        var (svc, lib, userData, _, dto) = MakeService();
        var userId = Guid.NewGuid();
        var seedMovie = new Movie { Id = Guid.NewGuid(), Name = "Inception", Genres = new[] { "Sci-Fi" } };
        var candidate = new Movie { Id = Guid.NewGuid(), Name = "Interstellar", Genres = new[] { "Sci-Fi" } };

        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 500)))
           .Returns(new List<BaseItem> { seedMovie });
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavoriteOrLiked == true)))
           .Returns(new List<BaseItem>());
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 6)))
           .Returns(new List<BaseItem> { seedMovie });
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q =>
                q.Genres != null && q.Genres.Count > 0 && q.IsPlayed != true)))
           .Returns(new List<BaseItem> { candidate });
        userData.Setup(u => u.GetUserData(It.IsAny<User>(), It.IsAny<BaseItem>()))
                .Returns(new UserItemData { Key = "k", Played = true });
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns<IReadOnlyList<BaseItem>, DtoOptions, User, BaseItem, bool>((items, _, _, _, _) => items.Select(i => new BaseItemDto { Id = i.Id, Name = i.Name }).ToList());

        var req = new RecommendationRequest(userId, BaseItemKind.Movie, ParentId: null, CategoryLimit: int.MaxValue, ItemLimit: int.MaxValue, new DtoOptions());

        await svc.GetRecommendationsAsync(req, CancellationToken.None);

        var limits = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => ((InternalItemsQuery)i.Arguments[0]).Limit)
            .ToList();
        Assert.All(limits, l => Assert.True(l is null or >= 0, $"Query Limit was negative: {l}"));
    }

    /// <summary>
    /// A pathologically large ranked-list limit must be clamped before the *6 candidate-pool
    /// multiplication so it never overflows int into a negative DB Limit.
    /// </summary>
    [Fact]
    public async Task GetRankedItemsAsync_HugeLimit_DoesNotProduceNegativeQueryLimit()
    {
        var (svc, lib, userData, _, dto) = MakeService();
        var userId = Guid.NewGuid();
        var watched = new Movie { Id = Guid.NewGuid(), Genres = new[] { "Sci-Fi" } };

        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == true && q.Limit == 500)))
           .Returns(new List<BaseItem> { watched });
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsFavoriteOrLiked == true && q.Limit == 250)))
           .Returns(new List<BaseItem>());
        lib.Setup(l => l.GetItemList(It.Is<InternalItemsQuery>(q => q.IsPlayed == false)))
           .Returns(new List<BaseItem>());
        dto.Setup(d => d.GetBaseItemDtos(It.IsAny<IReadOnlyList<BaseItem>>(), It.IsAny<DtoOptions>(), It.IsAny<User>(), It.IsAny<BaseItem>(), It.IsAny<bool>()))
           .Returns(Array.Empty<BaseItemDto>());

        await svc.GetRankedItemsAsync(
            userId,
            BaseItemKind.Movie,
            parentId: null,
            startIndex: null,
            limit: int.MaxValue,
            enableTotalRecordCount: false,
            new DtoOptions(),
            CancellationToken.None);

        var poolLimit = lib.Invocations
            .Where(i => i.Method.Name == nameof(ILibraryManager.GetItemList))
            .Select(i => (InternalItemsQuery)i.Arguments[0])
            .Where(q => q.IsPlayed == false)
            .Select(q => q.Limit)
            .First();
        Assert.NotNull(poolLimit);
        Assert.True(poolLimit >= 0, $"Ranked candidate-pool Limit was negative: {poolLimit}");
    }
}
