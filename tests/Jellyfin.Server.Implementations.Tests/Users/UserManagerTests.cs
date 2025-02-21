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
        [InlineData("0@_-' .")]
        [InlineData("Aa0@_-' .+")]
        [InlineData("thisisa+testemail@test.foo")]
        [InlineData("------@@@--+++----@@--abcdefghijklmn---------@----_-_-___-_ .9foo+")]
        public void ThrowIfInvalidUsername_WhenValidUsername_DoesNotThrowArgumentException(string username)
        {
            var ex = Record.Exception(() => UserManager.ThrowIfInvalidUsername(username));
            Assert.Null(ex);
        }

        [Theory]
        [InlineData(" ")]
        [InlineData("")]
        [InlineData("special characters like & $ ? are not allowed")]
        [InlineData("thishasaspaceontheend ")]
        [InlineData(" thishasaspaceatthestart")]
        [InlineData(" thishasaspaceatbothends ")]
        [InlineData(" this has a space at both ends and inbetween ")]
        public void ThrowIfInvalidUsername_WhenInvalidUsername_ThrowsArgumentException(string username)
        {
            Assert.Throws<ArgumentException>(() => UserManager.ThrowIfInvalidUsername(username));
        }
    }
}
