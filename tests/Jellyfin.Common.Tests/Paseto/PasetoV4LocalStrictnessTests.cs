using System;
using System.Text;
using Jellyfin.Paseto;
using Xunit;

namespace Jellyfin.Common.Tests.Paseto
{
    /// <summary>
    /// Strictness tests: the implementation must reject padding, non-zero trailing bits,
    /// wrong headers, wrong key lengths, truncated tokens and bogus segments.
    /// </summary>
    public static class PasetoV4LocalStrictnessTests
    {
        private static readonly byte[] Key = { 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x8b, 0x8c, 0x8d, 0x8e, 0x8f };

        private static readonly byte[] Nonce = new byte[32];

        private static string ValidToken => PasetoV4Local.EncryptWithNonce(Key, "payload-content", Nonce);

        /// <summary>
        /// Base64url segments using '=' padding must be rejected.
        /// </summary>
        [Fact]
        public static void EqualsPadding_Rejected()
        {
            // Direct decoder: any '=' is illegal.
            Assert.Throws<FormatException>(() => Base64Url.Decode("YWJjZA=="));
            Assert.Throws<FormatException>(() => Base64Url.Decode("AA=="));
            Assert.Throws<FormatException>(() => Base64Url.Decode("=AAA"));

            // Token level: padding in the body segment.
            string token = ValidToken + "==";
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, token));

            // Padding in the footer segment.
            string footered = PasetoV4Local.EncryptWithNonce(Key, "payload-content", Nonce, "footer");
            string paddedFooter = footered + "==";
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, paddedFooter));
        }

        /// <summary>
        /// Base64url with non-zero trailing bits must be rejected.
        /// </summary>
        [Fact]
        public static void NonZeroTrailingBits_Rejected()
        {
            // 2 trailing bits (input length mod 4 == 2).
            byte[] oneByte = Base64Url.Decode("YQ"); // "a"
            Assert.Equal(new byte[] { 0x61 }, oneByte);
            Assert.Throws<FormatException>(() => Base64Url.Decode("YR")); // last char value has low 4 bits set

            // 4 trailing bits (input length mod 4 == 3).
            Assert.Equal(new byte[] { 0x61, 0x61 }, Base64Url.Decode("YWE")); // "aa"
            Assert.Throws<FormatException>(() => Base64Url.Decode("YWF"));

            // A single character cannot encode a whole byte.
            Assert.Throws<FormatException>(() => Base64Url.Decode("a"));

            // Token level: a 64-byte body encodes to 86 unpadded chars (rem == 2);
            // change the last char so its four trailing bits are non-zero.
            string body = Base64Url.Encode(new byte[64]);
            Assert.Equal(86, body.Length);
            Assert.Equal('A', body[85]);
            string mutatedBody = body[..85] + "B";
            string mutatedToken = "v4.local." + mutatedBody;
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, mutatedToken));
        }

        /// <summary>
        /// Known-answer tests for the base64url transform itself (RFC 4648 examples).
        /// </summary>
        [Fact]
        public static void Base64Url_KnownAnswers()
        {
            Assert.Equal("Zm9vYmFy", Base64Url.Encode(Encoding.ASCII.GetBytes("foobar")));
            Assert.Equal("YW55IGNhcm5hbCBwbGVhc3VyZS4", Base64Url.Encode(Encoding.ASCII.GetBytes("any carnal pleasure.")));
            Assert.Equal("-_8", Base64Url.Encode(new byte[] { 0xfb, 0xff }));
            Assert.Equal("foobar", Encoding.ASCII.GetString(Base64Url.Decode("Zm9vYmFy")));
        }

        /// <summary>
        /// Wrong or missing version headers must be rejected.
        /// </summary>
        [Fact]
        public static void WrongOrMissingHeader_Rejected()
        {
            string body = ValidToken[9..];

            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "v3.local." + body));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "v4.public." + body));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "local." + body));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, string.Empty));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "v4.local"));
        }

        /// <summary>
        /// Keys that are not exactly 32 bytes must be rejected on both encrypt and decrypt.
        /// </summary>
        [Fact]
        public static void WrongKeyLength_Rejected()
        {
            var shortKey = new byte[16];
            var longKey = new byte[64];
            var validToken = ValidToken;

            Assert.Throws<PasetoException>(() => PasetoV4Local.Encrypt(shortKey, "payload"));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Encrypt(longKey, "payload"));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(shortKey, validToken));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(longKey, validToken));
        }

        /// <summary>
        /// Truncated tokens (missing tag bytes, missing ciphertext, missing everything) must be rejected.
        /// </summary>
        [Fact]
        public static void TruncatedToken_Rejected()
        {
            var validToken = ValidToken;

            // Every strict prefix (short of the full token) must fail.
            for (int length = 8; length < validToken.Length; length += 7)
            {
                string prefix = validToken[..length];
                Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, prefix));
            }

            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "v4.local."));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, "v4.local.AAAA"));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, validToken[..^1]));
        }

        /// <summary>
        /// A bogus trailing segment must be rejected.
        /// </summary>
        [Fact]
        public static void BogusTrailingSegment_Rejected()
        {
            var validToken = ValidToken;

            // Footer segment that is not valid base64url.
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, validToken + ".bogus!"));

            // Footer segment with an impossible base64url length.
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, validToken + ".bogus"));

            // Footer segment that is valid base64url but invalid UTF-8.
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, validToken + ".AAAA"));

            // Footer segment that changes the authenticated footer.
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, validToken + ".Zm9vYmFy"));
        }

        /// <summary>
        /// Oversized tokens must be rejected before any decoding work.
        /// </summary>
        [Fact]
        public static void OversizedToken_Rejected()
        {
            string hugeBody = "v4.local." + new string('A', 2_000_000);
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, hugeBody));

            string hugeFooter = ValidToken + "." + new string('A', 1_500_000);
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, hugeFooter));
        }

        /// <summary>
        /// An empty payload is legal per the specification and must round-trip.
        /// </summary>
        [Fact]
        public static void EmptyPayload_RoundTrips()
        {
            string token = PasetoV4Local.EncryptWithNonce(Key, string.Empty, Nonce);
            Assert.StartsWith("v4.local.", token, StringComparison.Ordinal);

            // 32-byte nonce + 0-byte ciphertext + 32-byte tag = 64 bytes => 86 unpadded chars.
            Assert.Equal(9 + 86, token.Length); // header "v4.local." is 9 chars + 86 unpadded base64url chars
            Assert.Equal(string.Empty, PasetoV4Local.Decrypt(Key, token));
        }
    }
}
