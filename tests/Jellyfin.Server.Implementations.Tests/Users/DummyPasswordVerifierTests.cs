using Jellyfin.Server.Implementations.Users;
using Xunit;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    public class DummyPasswordVerifierTests
    {
        [Fact]
        public void Constructor_DoesNotCreateHash()
        {
            var cryptoProvider = new CountingCryptoProvider();

            _ = new DummyPasswordVerifier(cryptoProvider);

            Assert.Equal(0, cryptoProvider.CreateCalls);
        }

        [Fact]
        public void Verify_CreatesHashOnceAndVerifiesAgainstIt()
        {
            var cryptoProvider = new CountingCryptoProvider();
            var verifier = new DummyPasswordVerifier(cryptoProvider);

            verifier.Verify("a");
            verifier.Verify("b");

            Assert.Equal(1, cryptoProvider.CreateCalls);
            Assert.Equal(2, cryptoProvider.VerifyCalls);
            Assert.Same(cryptoProvider.LastCreatedHash, cryptoProvider.LastVerifiedHash);
        }
    }
}
