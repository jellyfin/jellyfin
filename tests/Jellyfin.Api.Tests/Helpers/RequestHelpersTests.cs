using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Helpers;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Net;
using MediaBrowser.Model.Querying;
using Xunit;

namespace Jellyfin.Api.Tests.Helpers
{
    public static class RequestHelpersTests
    {
        [Theory]
        [MemberData(nameof(GetOrderBy_Success_TestData))]
        public static void GetOrderBy_Success(IReadOnlyList<ItemSortBy> sortBy, IReadOnlyList<SortOrder> requestedSortOrder, (ItemSortBy, SortOrder)[] expected)
        {
            Assert.Equal(expected, RequestHelpers.GetOrderBy(sortBy, requestedSortOrder));
        }

        [Fact]
        public static void GetUserId_IsAdmin()
        {
            Guid? requestUserId = Guid.NewGuid();
            Guid? authUserId = Guid.NewGuid();

            var claims = new[]
            {
                new Claim(InternalClaimTypes.UserId, authUserId.Value.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.IsApiKey, bool.FalseString),
                new Claim(ClaimTypes.Role, UserRoles.Administrator)
            };

            var identity = new ClaimsIdentity(claims, string.Empty);
            var principal = new ClaimsPrincipal(identity);

            var userId = RequestHelpers.GetUserId(principal, requestUserId);

            Assert.Equal(requestUserId, userId);
        }

        [Fact]
        public static void GetUserId_IsApiKey_EmptyGuid()
        {
            Guid? requestUserId = Guid.Empty;

            var claims = new[]
            {
                new Claim(InternalClaimTypes.IsApiKey, bool.TrueString)
            };

            var identity = new ClaimsIdentity(claims, string.Empty);
            var principal = new ClaimsPrincipal(identity);

            var userId = RequestHelpers.GetUserId(principal, requestUserId);

            Assert.Equal(Guid.Empty, userId);
        }

        [Fact]
        public static void GetUserId_IsApiKey_Null()
        {
            Guid? requestUserId = null;

            var claims = new[]
            {
                new Claim(InternalClaimTypes.IsApiKey, bool.TrueString)
            };

            var identity = new ClaimsIdentity(claims, string.Empty);
            var principal = new ClaimsPrincipal(identity);

            var userId = RequestHelpers.GetUserId(principal, requestUserId);

            Assert.Equal(Guid.Empty, userId);
        }

        [Fact]
        public static void GetUserId_IsUser()
        {
            Guid? requestUserId = Guid.NewGuid();
            Guid? authUserId = Guid.NewGuid();

            var claims = new[]
            {
                new Claim(InternalClaimTypes.UserId, authUserId.Value.ToString("N", CultureInfo.InvariantCulture)),
                new Claim(InternalClaimTypes.IsApiKey, bool.FalseString),
                new Claim(ClaimTypes.Role, UserRoles.User)
            };

            var identity = new ClaimsIdentity(claims, string.Empty);
            var principal = new ClaimsPrincipal(identity);

            Assert.Throws<SecurityException>(() => RequestHelpers.GetUserId(principal, requestUserId));
        }

        [Fact]
        public static void ApplyItemFilterConstraints_IsFolderOnly_SetsIsFolderTrue()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsFolder };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.True(query.IsFolder);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_IsNotFolderOnly_SetsIsFolderFalse()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsNotFolder };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.False(query.IsFolder);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_BothIsFolderAndIsNotFolder_LeavesIsFolderNull()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsFolder, ItemFilter.IsNotFolder };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.Null(query.IsFolder);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_BothIsFolderAndIsNotFolder_ReversedOrder_LeavesIsFolderNull()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsNotFolder, ItemFilter.IsFolder };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.Null(query.IsFolder);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_ContradictoryFolderFilters_OtherFiltersStillApplied()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsFolder, ItemFilter.IsNotFolder, ItemFilter.IsFavorite, ItemFilter.IsPlayed };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.Null(query.IsFolder);
            Assert.True(query.IsFavorite);
            Assert.True(query.IsPlayed);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_NoFilters_LeavesQueryUnchanged()
        {
            var query = new InternalItemsQuery();
            var filters = Array.Empty<ItemFilter>();

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.Null(query.IsFolder);
            Assert.Null(query.IsFavorite);
            Assert.Null(query.IsPlayed);
            Assert.Null(query.IsLiked);
        }

        [Fact]
        public static void ApplyItemFilterConstraints_AllNonContradictoryFilters_AppliedCorrectly()
        {
            var query = new InternalItemsQuery();
            var filters = new[] { ItemFilter.IsFavorite, ItemFilter.IsResumable, ItemFilter.Likes };

            RequestHelpers.ApplyItemFilterConstraints(query, filters);

            Assert.True(query.IsFavorite);
            Assert.True(query.IsResumable);
            Assert.True(query.IsLiked);
            Assert.Null(query.IsFolder);
        }

        public static TheoryData<IReadOnlyList<ItemSortBy>, IReadOnlyList<SortOrder>, (ItemSortBy, SortOrder)[]> GetOrderBy_Success_TestData()
        {
            var data = new TheoryData<IReadOnlyList<ItemSortBy>, IReadOnlyList<SortOrder>, (ItemSortBy, SortOrder)[]>();

            data.Add(
                Array.Empty<ItemSortBy>(),
                Array.Empty<SortOrder>(),
                Array.Empty<(ItemSortBy, SortOrder)>());

            data.Add(
                new[]
                {
                    ItemSortBy.IsFavoriteOrLiked,
                    ItemSortBy.Random
                },
                Array.Empty<SortOrder>(),
                new (ItemSortBy, SortOrder)[]
                {
                    (ItemSortBy.IsFavoriteOrLiked, SortOrder.Ascending),
                    (ItemSortBy.Random, SortOrder.Ascending),
                });

            data.Add(
                new[]
                {
                    ItemSortBy.SortName,
                    ItemSortBy.ProductionYear
                },
                new[]
                {
                    SortOrder.Descending
                },
                new (ItemSortBy, SortOrder)[]
                {
                    (ItemSortBy.SortName, SortOrder.Descending),
                    (ItemSortBy.ProductionYear, SortOrder.Descending),
                });

            return data;
        }
    }
}
