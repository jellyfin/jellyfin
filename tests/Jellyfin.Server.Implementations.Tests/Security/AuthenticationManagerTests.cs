using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Security;
using Jellyfin.Server.Implementations.Tests.Item;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Security
{
    public sealed class AuthenticationManagerTests : JellyfinDbTestFixture
    {
        private readonly AuthenticationManager _authenticationManager;

        public AuthenticationManagerTests()
        {
            _authenticationManager = new AuthenticationManager(CreateDbContextFactory());
        }

        [Fact]
        public async Task CreateApiKey_ReturnsInfoMatchingStoredKey()
        {
            var created = await _authenticationManager.CreateApiKey("test-app");

            Assert.Equal("test-app", created.AppName);
            Assert.False(string.IsNullOrEmpty(created.AccessToken));

            var keys = await _authenticationManager.GetApiKeys();
            var stored = Assert.Single(keys);
            Assert.Equal(created.AppName, stored.AppName);
            Assert.Equal(created.AccessToken, stored.AccessToken);
        }

        [Fact]
        public async Task CreateApiKey_WithMultipleKeys_ReturnsDistinctTokens()
        {
            var first = await _authenticationManager.CreateApiKey("app1");
            var second = await _authenticationManager.CreateApiKey("app2");

            Assert.NotEqual(first.AccessToken, second.AccessToken);
        }
    }
}
