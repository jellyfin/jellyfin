using System;
using System.Linq;
using System.Text.Json;
using MediaBrowser.Model.Dto;
using Xunit;

namespace Jellyfin.Api.Tests.Controllers
{
    public class UserDtoShareLinkKeyTests
    {
        [Fact]
        public void UserDto_DoesNotExposeShareLinkKey()
        {
            Assert.DoesNotContain(
                typeof(UserDto).GetProperties(),
                property => property.Name.Contains("ShareLinkKey", StringComparison.OrdinalIgnoreCase));

            var user = new UserDto
            {
                Name = "test",
                Id = Guid.NewGuid()
            };

            var json = JsonSerializer.Serialize(user);

            Assert.DoesNotContain("ShareLinkKey", json, StringComparison.OrdinalIgnoreCase);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void UserDto_ExposesEffectiveCanShareMediaFlag(bool canShareMedia)
        {
            var user = new UserDto
            {
                Name = "test",
                Id = Guid.NewGuid(),
                CanShareMedia = canShareMedia
            };

            var json = JsonSerializer.Serialize(user);

            // The effective flag is a UI hint only; it must be serialized so clients can
            // hide share / copy-stream actions instead of hitting a 403.
            Assert.Contains("\"CanShareMedia\":" + (canShareMedia ? "true" : "false"), json, StringComparison.Ordinal);

            // It must never carry the key material itself.
            Assert.DoesNotContain("ShareLinkKey", json, StringComparison.OrdinalIgnoreCase);
        }
    }
}
