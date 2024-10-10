using System;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class UserManagerTests
    {
        [Theory]
        [InlineData("this_is_valid")]
        [InlineData("this is also valid")]
        [InlineData("this+too")]
        [InlineData("0@_-' .")]
        [InlineData("john+doe")]
        [InlineData("JöhnDøë")]
        [InlineData("Jö hn+Døë")]
        [InlineData("Jö hn+Døë@")]
        public void ThrowIfInvalidUsername_WhenValidUsername_DoesNotThrowArgumentException(string username)
        {
            var ex = Record.Exception(() => UserManager.ThrowIfInvalidUsername(username));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("")]
        [InlineData("special characters like & $ ? are not allowed")]
        public void ThrowIfInvalidUsername_WhenInvalidUsername_ThrowsArgumentException(string username)
        {
            Assert.Throws<ArgumentException>(() => UserManager.ThrowIfInvalidUsername(username));
        }
    }
}
