using System;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class UserValidationTests
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
            var userValidation = new UserValidation();

            var ex = Record.Exception(() => userValidation.ThrowIfInvalidUsername(username));
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
            var userValidation = new UserValidation();

            Assert.Throws<ArgumentException>(() => userValidation.ThrowIfInvalidUsername(username));
        }
    }
}
