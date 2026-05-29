using System;
using System.Collections.Generic;
using System.Linq;
using Jellyfin.Database.Implementations.Entities;
using Xunit;

namespace Jellyfin.Controller.Tests
{
    public class PlaybackActivityTests
    {
        [Fact]
        public void GetCorrectMoviesForUser()
        {
            var userID = Guid.NewGuid();
            var testData = new List<PlaybackActivity>
            {
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 1",
                    MediaType = "Movie",
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = Guid.NewGuid(),
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 2",
                    MediaType = "Movie",
                }
            }.AsQueryable();

            // Test query logic
            var result = testData
                .Where(p => p.UserId.Equals(userID) && p.MediaType == "Movie")
                .ToList();

            Assert.Single(result);
            Assert.Equal("Movie 1", result[0].ItemName);
        }

        [Fact]
        public void GetCorrectMediaTypesForUser()
        {
            var userID = Guid.NewGuid();
            var testData = new List<PlaybackActivity>
            {
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 1",
                    MediaType = "Movie",
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Episode 1",
                    MediaType = "Episode",
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Song 123",
                    MediaType = "Song",
                }
            }.AsQueryable();

            var result = testData
                .Where(p => p.UserId.Equals(userID))
                .GroupBy(p => p.MediaType)
                .Select(g => new
                {
                    MediaType = g.Key,
                    TotalTicks = g.Sum(p => p.PlayedTicks),
                    PlayCount = g.Count()
                })
                .ToList();

            Assert.Equal(3, result.Count);
            Assert.Contains(result, r => r.MediaType == "Movie");
            Assert.Contains(result, r => r.MediaType == "Episode");
            Assert.Contains(result, r => r.MediaType == "Song");
        }

        [Fact]
        public void GetCorrectSubGroupsForMediaType()
        {
            var userID = Guid.NewGuid();
            var testData = new List<PlaybackActivity>
            {
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 1",
                    MediaType = "Movie",
                    ItemSubGroup = "Action"
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 2",
                    MediaType = "Movie",
                    ItemSubGroup = "Comedy"
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID,
                    ItemId = Guid.NewGuid(),
                    ItemName = "NEWMovie 3",
                    MediaType = "Movie",
                    ItemSubGroup = "Drama"
                }
            }.AsQueryable();

            var result = testData
                .Where(p => p.UserId.Equals(userID))
                .GroupBy(p => p.MediaType)
                .Select(g => new
                {
                    MediaType = g.Key,
                    TotalTicks = g.Sum(p => p.PlayedTicks),
                    PlayCount = g.Count()
                })
                .ToList();

            // Only movies
            Assert.Single(result);

            var result2 = testData
                .Where(p => p.UserId.Equals(userID) && p.MediaType == "Movie")
                .GroupBy(p => p.ItemSubGroup)
                .Select(g => new
                {
                    SubGroup = g.Key,
                    TotalTicks = g.Sum(p => p.PlayedTicks),
                    PlayCount = g.Count()
                })
                .ToList();

            Assert.Equal(3, result2.Count);
            Assert.Contains(result2, r => r.SubGroup == "Action");
            Assert.Contains(result2, r => r.SubGroup == "Comedy");
            Assert.Contains(result2, r => r.SubGroup == "Drama");
        }

        [Fact]
        public void GetDifferentResultsForDifferentUsers()
        {
            var userID1 = Guid.NewGuid();
            var userID2 = Guid.NewGuid();
            var testData = new List<PlaybackActivity>
            {
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID1,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 1",
                    MediaType = "Movie"
                },
                new PlaybackActivity
                {
                    Id = Guid.NewGuid(),
                    UserId = userID1,
                    ItemId = Guid.NewGuid(),
                    ItemName = "Movie 2",
                    MediaType = "Movie"
                }
            }.AsQueryable();

            var result1 = testData
                .Where(p => p.UserId.Equals(userID1))
                .ToList();

            var result2 = testData
                .Where(p => p.UserId.Equals(userID2))
                .ToList();

            Assert.Equal(2, result1.Count);
            Assert.Contains(result1, r => r.MediaType == "Movie");
            Assert.Empty(result2);
        }
    }
}
