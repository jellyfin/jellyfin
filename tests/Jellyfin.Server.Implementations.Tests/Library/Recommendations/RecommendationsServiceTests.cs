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
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Library.Recommendations;
using MediaBrowser.Controller.Persistence;
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
}
