using MediaBrowser.Common;
using MediaBrowser.Common.Cryptography;
using Xunit;

namespace Jellyfin.Common.Tests
{
    public class PasswordHashTests
    {
        [Theory]
        [InlineData("$PBKDF2$iterations=1000$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
            "PBKDF2",
            "",
            "62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        public void ParseTest(string passwordHash, string id, string salt, string hash)
        {
            var pass = PasswordHash.Parse(passwordHash);
            Assert.Equal(id, pass.Id);
            Assert.Equal(salt, Hex.Encode(pass.Salt, false));
            Assert.Equal(hash, Hex.Encode(pass.Hash, false));
        }

        [Theory]
        [InlineData("$PBKDF2$iterations=1000$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        public void ToStringTest(string passwordHash)
        {
            Assert.Equal(passwordHash, PasswordHash.Parse(passwordHash).ToString());
        }
    }
}
