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
    }
}
