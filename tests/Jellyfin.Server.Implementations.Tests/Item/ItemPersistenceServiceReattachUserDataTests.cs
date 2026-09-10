using System;
using System.Data.Common;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Emby.Server.Implementations.Library;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Item;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Model.Configuration;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using AudioBook = MediaBrowser.Controller.Entities.AudioBook;

namespace Jellyfin.Server.Implementations.Tests.Item;

public sealed class ItemPersistenceServiceReattachUserDataTests : SqliteDbTestFixture
{
    private readonly ItemPersistenceService _service;
    private readonly CommitFailureInterceptor _commitFailure;
    private readonly AudioBook _item = new()
    {
        Id = Guid.NewGuid(),
        Name = "Book",
        Album = "Series",
        AlbumArtists = ["Author"],
        IndexNumber = 1
    };

    private readonly User _user = new("user", "auth-provider", "reset-provider");

    public ItemPersistenceServiceReattachUserDataTests()
        : this(new CommitFailureInterceptor())
    {
    }

    private ItemPersistenceServiceReattachUserDataTests(CommitFailureInterceptor commitFailure)
        : base(commitFailure)
    {
        _commitFailure = commitFailure;
        _service = new ItemPersistenceService(
            CreateDbContextFactory(),
            Mock.Of<IServerApplicationHost>(),
            NullLogger<ItemPersistenceService>.Instance);

        using var context = CreateDbContext();
        context.Users.Add(_user);
        context.BaseItems.Add(new BaseItemEntity { Id = _item.Id, Type = typeof(AudioBook).FullName! });
        context.SaveChanges();
    }

    [Fact]
    public async Task ReattachUserDataAsync_RetainedOnly_MovesMatchingRowsAndRefreshesItem()
    {
        var retained = CreateRetainedRow(_user.Id, _item.GetUserDataKeys()[0]);
        var unrelated = CreateRetainedRow(_user.Id, "unrelated-key");
        Seed(retained, unrelated);

        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        AssertRow(retained, Assert.Single(context.UserData.Where(e => e.ItemId.Equals(_item.Id))), _item.Id);
        AssertRow(unrelated, Assert.Single(context.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId))));
        AssertRow(retained, Assert.Single(_item.UserData), _item.Id);
    }

    [Fact]
    public async Task ReattachUserDataAsync_ExistingDestination_PreservesBothRowsOnRepeatedCalls()
    {
        var retained = CreateRetainedRow(_user.Id, _item.GetUserDataKeys()[0]);
        var current = CreateCurrentRow(retained.CustomDataKey);
        Seed(retained, current);

        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);
        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);

        using var context = CreateDbContext();
        Assert.Equal(2, context.UserData.Count());
        AssertRow(retained, Assert.Single(context.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId))));
        AssertRow(current, Assert.Single(context.UserData.Where(e => e.ItemId.Equals(_item.Id))));
        AssertRow(current, Assert.Single(_item.UserData));
    }

    [Fact]
    public async Task ReattachUserDataAsync_MixedUsersAndKeys_SkipsOnlyTheConflictingTuple()
    {
        var otherUser = new User("other-user", "auth-provider", "reset-provider");
        using (var context = CreateDbContext())
        {
            context.Users.Add(otherUser);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        var keys = _item.GetUserDataKeys();
        var conflicting = CreateRetainedRow(_user.Id, keys[0]);
        var current = CreateCurrentRow(keys[0]);
        var otherKey = CreateRetainedRow(_user.Id, keys[1]);
        var otherUsersPrimary = CreateRetainedRow(otherUser.Id, keys[0]);
        var otherUsersFallback = CreateRetainedRow(otherUser.Id, keys[1]);
        Seed(conflicting, current, otherKey, otherUsersPrimary, otherUsersFallback);

        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);

        using var after = CreateDbContext();
        Assert.Equal(5, after.UserData.Count());
        AssertRow(conflicting, Assert.Single(after.UserData.Where(e => e.ItemId.Equals(BaseItemRepository.PlaceholderId))));
        var attached = after.UserData.Where(e => e.ItemId.Equals(_item.Id)).ToDictionary(e => (e.UserId, e.CustomDataKey));
        Assert.Equal(4, attached.Count);
        AssertRow(current, attached[(_user.Id, keys[0])]);
        foreach (var row in new[] { otherKey, otherUsersPrimary, otherUsersFallback })
        {
            AssertRow(row, attached[(row.UserId, row.CustomDataKey)], _item.Id);
        }

        Assert.Equal(attached.Count, _item.UserData.Count);
        foreach (var row in _item.UserData)
        {
            AssertRow(attached[(row.UserId, row.CustomDataKey)], row);
        }
    }

    [Fact]
    public async Task ReattachUserDataAsync_NoRetainedRows_RefreshesExistingItemData()
    {
        var current = CreateCurrentRow(_item.GetUserDataKeys()[0]);
        Seed(current);

        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);

        AssertRow(current, Assert.Single(_item.UserData));
    }

    [Fact]
    public async Task ReattachUserDataAsync_Cancelled_DoesNotChangeDatabaseOrItemData()
    {
        var retained = CreateRetainedRow(_user.Id, _item.GetUserDataKeys()[0]);
        Seed(retained);
        _item.UserData = [CreateCurrentRow(retained.CustomDataKey)];
        var originalItemData = _item.UserData;
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ReattachUserDataAsync(_item, cancellation.Token));

        using var context = CreateDbContext();
        AssertRow(retained, Assert.Single(context.UserData));
        Assert.Same(originalItemData, _item.UserData);
    }

    [Fact]
    public async Task ReattachUserDataAsync_CommitFails_RollsBackAndKeepsOriginalItemData()
    {
        var retained = CreateRetainedRow(_user.Id, _item.GetUserDataKeys()[0]);
        Seed(retained);
        _item.UserData = [CreateCurrentRow(retained.CustomDataKey)];
        var originalItemData = _item.UserData;
        _commitFailure.FailCommit = true;

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken));

        Assert.Equal("Injected commit failure", exception.Message);
        Assert.Equal(1, _commitFailure.CommitAttempts);
        using var context = CreateDbContext();
        AssertRow(retained, Assert.Single(context.UserData));
        Assert.Same(originalItemData, _item.UserData);
    }

    [Fact]
    public async Task ReattachUserDataAsync_CancelledAtCommit_RollsBackAndKeepsOriginalItemData()
    {
        var retained = CreateRetainedRow(_user.Id, _item.GetUserDataKeys()[0]);
        Seed(retained);
        _item.UserData = [CreateCurrentRow(retained.CustomDataKey)];
        var originalItemData = _item.UserData;
        using var cancellation = new CancellationTokenSource();
        _commitFailure.CancelAtCommit = cancellation;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => _service.ReattachUserDataAsync(_item, cancellation.Token));

        Assert.True(cancellation.IsCancellationRequested);
        Assert.Equal(1, _commitFailure.CommitAttempts);
        using var context = CreateDbContext();
        AssertRow(retained, Assert.Single(context.UserData));
        Assert.Same(originalItemData, _item.UserData);
    }

    [Fact]
    public async Task ReattachUserDataAsync_RetainedPrimaryAndCurrentFallback_PreservesExistingKeyPrecedence()
    {
        var keys = _item.GetUserDataKeys();
        var retained = CreateRetainedRow(_user.Id, keys[0]);
        var current = CreateCurrentRow(keys[1]);
        Seed(retained, current);

        await _service.ReattachUserDataAsync(_item, TestContext.Current.CancellationToken);

        Assert.Equal(2, _item.UserData.Count);
        AssertRow(current, Assert.Single(_item.UserData, e => e.CustomDataKey == keys[1]));
        AssertRow(retained, Assert.Single(_item.UserData, e => e.CustomDataKey == keys[0]), _item.Id);

        // Reattachment preserves rows by key; the existing read path still prefers the primary key.
        var configuration = new Mock<IServerConfigurationManager>();
        configuration.SetupGet(e => e.Configuration).Returns(new ServerConfiguration());
        var manager = new UserDataManager(configuration.Object, CreateDbContextFactory());
        var resolved = manager.GetUserData(_user, _item);
        Assert.NotNull(resolved);
        Assert.Equal(keys[0], resolved.Key);
        Assert.Equal(retained.PlaybackPositionTicks, resolved.PlaybackPositionTicks);
    }

    private static UserData CreateRetainedRow(Guid userId, string key) => new()
    {
        ItemId = BaseItemRepository.PlaceholderId,
        Item = null,
        UserId = userId,
        User = null,
        CustomDataKey = key,
        RetentionDate = new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc),
        IsFavorite = false,
        Rating = 2,
        Likes = false,
        PlaybackPositionTicks = 1234,
        PlayCount = 1,
        Played = false,
        LastPlayedDate = new DateTime(2026, 8, 1, 0, 0, 0, DateTimeKind.Utc),
        AudioStreamIndex = 1,
        SubtitleStreamIndex = 2
    };

    private UserData CreateCurrentRow(string key) => new()
    {
        ItemId = _item.Id,
        Item = null,
        UserId = _user.Id,
        User = null,
        CustomDataKey = key,
        IsFavorite = true,
        Rating = 9,
        Likes = true,
        PlaybackPositionTicks = 9876,
        PlayCount = 3,
        Played = true,
        LastPlayedDate = new DateTime(2026, 9, 8, 0, 0, 0, DateTimeKind.Utc),
        AudioStreamIndex = 3,
        SubtitleStreamIndex = 4
    };

    private static void AssertRow(UserData expected, UserData actual, Guid? attachedItemId = null)
    {
        Assert.Equal(attachedItemId ?? expected.ItemId, actual.ItemId);
        Assert.Equal(attachedItemId.HasValue ? null : expected.RetentionDate, actual.RetentionDate);
        Assert.Equal(expected.UserId, actual.UserId);
        Assert.Equal(expected.CustomDataKey, actual.CustomDataKey);
        Assert.Equal(expected.IsFavorite, actual.IsFavorite);
        Assert.Equal(expected.Rating, actual.Rating);
        Assert.Equal(expected.Likes, actual.Likes);
        Assert.Equal(expected.PlaybackPositionTicks, actual.PlaybackPositionTicks);
        Assert.Equal(expected.PlayCount, actual.PlayCount);
        Assert.Equal(expected.Played, actual.Played);
        Assert.Equal(expected.LastPlayedDate, actual.LastPlayedDate);
        Assert.Equal(expected.AudioStreamIndex, actual.AudioStreamIndex);
        Assert.Equal(expected.SubtitleStreamIndex, actual.SubtitleStreamIndex);
    }

    private void Seed(params UserData[] rows)
    {
        using var context = CreateDbContext();
        context.UserData.AddRange(rows);
        context.SaveChanges();
    }

    private sealed class CommitFailureInterceptor : DbTransactionInterceptor
    {
        public bool FailCommit { get; set; }

        public CancellationTokenSource? CancelAtCommit { get; set; }

        public int CommitAttempts { get; private set; }

        public override async ValueTask<InterceptionResult> TransactionCommittingAsync(
            DbTransaction transaction,
            TransactionEventData eventData,
            InterceptionResult result,
            CancellationToken cancellationToken = default)
        {
            CommitAttempts++;
            if (FailCommit)
            {
                throw new InvalidOperationException("Injected commit failure");
            }

            if (CancelAtCommit is not null)
            {
                await CancelAtCommit.CancelAsync();
                cancellationToken.ThrowIfCancellationRequested();
            }

            return await base.TransactionCommittingAsync(transaction, eventData, result, cancellationToken);
        }
    }
}
