using System;
using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class UserManagerTests
    {
        [Theory]
        [InlineData("this_is_valid", true)]
        [InlineData("this is also valid", true)]
        [InlineData(" ", false)]
        [InlineData("", false)]
        [InlineData("0@_-' .", true)]
        public void ThrowIfInvalidUsername_WhenInvalidUsername_ThrowsArgumentException(string username, bool isValid)
        {
            var ex = Record.Exception(() => UserManager.ThrowIfInvalidUsername(username));

            var argumentExceptionNotThrown = ex is not ArgumentException;
            if (ex != null)
            {
                Assert.Equal(typeof(ArgumentException), ex.GetType());
            }

            Assert.Equal(isValid, argumentExceptionNotThrown);
        }
    }
}
