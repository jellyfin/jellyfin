using System;
using System.Collections.Generic;
using Emby.Server.Implementations.Sorting;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Library;
using Moq;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Sorting
{
    public class UserDataBatchComparerTests
    {
        private static readonly User _user = new("comparer-test", "provider", "reset");

        [Fact]
        public void DatePlayedComparer_WithBatch_DoesNotQueryPerItem()
        {
            var older = new Movie { Id = Guid.NewGuid() };
            var newer = new Movie { Id = Guid.NewGuid() };
            var comparer = new DatePlayedComparer
            {
                User = _user,
                UserDataManager = new Mock<IUserDataManager>(MockBehavior.Strict).Object,
                UserDataById = new Dictionary<Guid, UserItemData>
                {
                    [older.Id] = new() { Key = "older", LastPlayedDate = new DateTime(2020, 1, 1) },
                    [newer.Id] = new() { Key = "newer", LastPlayedDate = new DateTime(2024, 1, 1) },
                }
            };

            Assert.True(comparer.Compare(older, newer) < 0);
        }

        [Fact]
        public void PlayCountComparer_WithBatch_DoesNotQueryPerItem()
        {
            var once = new Movie { Id = Guid.NewGuid() };
            var twice = new Movie { Id = Guid.NewGuid() };
            var comparer = new PlayCountComparer
            {
                User = _user,
                UserDataManager = new Mock<IUserDataManager>(MockBehavior.Strict).Object,
                UserDataById = new Dictionary<Guid, UserItemData>
                {
                    [once.Id] = new() { Key = "once", PlayCount = 1 },
                    [twice.Id] = new() { Key = "twice", PlayCount = 2 },
                }
            };

            Assert.True(comparer.Compare(once, twice) < 0);
        }
    }
}
