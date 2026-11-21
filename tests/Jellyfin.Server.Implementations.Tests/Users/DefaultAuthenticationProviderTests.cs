using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Authentication;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class DefaultAuthenticationProviderTests
    {
        private readonly CountingCryptoProvider _cryptoProvider;
        private readonly DefaultAuthenticationProvider _provider;

        public DefaultAuthenticationProviderTests()
        {
            _cryptoProvider = new CountingCryptoProvider();
            _provider = new DefaultAuthenticationProvider(NullLogger<DefaultAuthenticationProvider>.Instance, _cryptoProvider, new DummyPasswordVerifier(_cryptoProvider));
        }

        [Fact]
        public async Task Authenticate_UnknownUser_VerifiesDummyHashAndThrows()
        {
            await Assert.ThrowsAsync<AuthenticationException>(() => _provider.Authenticate("nobody", "secret", null));

            Assert.Equal(1, _cryptoProvider.VerifyCalls);
        }

        [Fact]
        public async Task Authenticate_UserWithoutPassword_WrongPassword_VerifiesDummyHashAndThrows()
        {
            var user = new User("someone", "default", "default");

            await Assert.ThrowsAsync<AuthenticationException>(() => _provider.Authenticate("someone", "secret", user));

            Assert.Equal(1, _cryptoProvider.VerifyCalls);
        }

        [Fact]
        public async Task Authenticate_UserWithoutPassword_EmptyPassword_Succeeds()
        {
            var user = new User("someone", "default", "default");

            var result = await _provider.Authenticate("someone", string.Empty, user);

            Assert.Equal("someone", result.Username);
            Assert.Equal(0, _cryptoProvider.VerifyCalls);
        }

        [Fact]
        public async Task Authenticate_UnknownUser_DummyHashMatches_StillThrows()
        {
            _cryptoProvider.VerifyResult = true;

            await Assert.ThrowsAsync<AuthenticationException>(() => _provider.Authenticate("nobody", "secret", null));
        }
    }
}
