using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoFixture;
using AutoFixture.AutoMoq;
using Jellyfin.Api.Auth.SyncPlayAccessPolicy;
using Jellyfin.Api.Constants;
using Jellyfin.Data.Enums;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Library;
using Microsoft.AspNetCore.Authorization;
using Moq;
using Xunit;

namespace Jellyfin.Api.Tests.Auth.SyncPlayAccessPolicy
{
    public class SyncPlayAccessHandlerTests
    {
        private readonly Mock<IUserManager> _userManagerMock;
        private readonly SyncPlayAccessHandler _sut;

        public SyncPlayAccessHandlerTests()
        {
            var fixture = new Fixture().Customize(new AutoMoqCustomization());
            _userManagerMock = fixture.Freeze<Mock<IUserManager>>();
            _sut = fixture.Create<SyncPlayAccessHandler>();
        }

        [Fact]
        public async Task ShouldNotSucceedWithoutUserId()
        {
            _userManagerMock
                .Setup(userManager => userManager.GetUserById(Guid.Empty))
                .Throws(new ArgumentException("Guid can't be empty", "id"));

            var requirement = new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.HasAccess);
            var context = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { requirement },
                new ClaimsPrincipal(new ClaimsIdentity()),
                null);

            await _sut.HandleAsync(context);

            Assert.False(context.HasSucceeded);
        }

        [Fact]
        public async Task ShouldThrowWhenUserDoesNotExist()
        {
            var userId = Guid.NewGuid();
            _userManagerMock
                .Setup(userManager => userManager.GetUserById(userId))
                .Returns<User?>(null);

            var requirement = new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.HasAccess);
            var context = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { requirement },
                CreatePrincipal(userId),
                null);

            await Assert.ThrowsAsync<ResourceNotFoundException>(() => _sut.HandleAsync(context));
        }

        [Fact]
        public async Task ShouldSucceedWhenUserCanJoinGroups()
        {
            var userId = Guid.NewGuid();
            var user = new User("jellyfin", "auth-provider", "reset-provider")
            {
                SyncPlayAccess = SyncPlayUserAccessType.JoinGroups
            };
            _userManagerMock
                .Setup(userManager => userManager.GetUserById(userId))
                .Returns(user);

            var requirement = new SyncPlayAccessRequirement(SyncPlayAccessRequirementType.HasAccess);
            var context = new AuthorizationHandlerContext(
                new List<IAuthorizationRequirement> { requirement },
                CreatePrincipal(userId),
                null);

            await _sut.HandleAsync(context);

            Assert.True(context.HasSucceeded);
        }

        private static ClaimsPrincipal CreatePrincipal(Guid userId)
        {
            var claims = new[]
            {
                new Claim(InternalClaimTypes.UserId, userId.ToString("N", CultureInfo.InvariantCulture))
            };

            return new ClaimsPrincipal(new ClaimsIdentity(claims));
        }
    }
}
