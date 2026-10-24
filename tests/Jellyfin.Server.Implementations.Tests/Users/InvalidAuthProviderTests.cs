using System.Threading.Tasks;
using Jellyfin.Server.Implementations.Users;
using MediaBrowser.Controller.Authentication;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class InvalidAuthProviderTests
    {
        private readonly CountingCryptoProvider _cryptoProvider;
        private readonly InvalidAuthProvider _provider;

        public InvalidAuthProviderTests()
        {
            _cryptoProvider = new CountingCryptoProvider();
            _provider = new InvalidAuthProvider(new DummyPasswordVerifier(_cryptoProvider));
        }

        [Fact]
        public async Task Authenticate_VerifiesDummyHashAndThrows()
        {
            await Assert.ThrowsAsync<AuthenticationException>(() => _provider.Authenticate("someone", "secret"));

            Assert.Equal(1, _cryptoProvider.VerifyCalls);
        }

        [Fact]
        public async Task Authenticate_DummyHashMatches_StillThrows()
        {
            _cryptoProvider.VerifyResult = true;

            await Assert.ThrowsAsync<AuthenticationException>(() => _provider.Authenticate("someone", "secret"));
        }
    }
}
