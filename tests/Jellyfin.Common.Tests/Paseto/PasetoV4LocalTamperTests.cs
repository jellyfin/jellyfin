using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Jellyfin.Paseto;
using Xunit;

namespace Jellyfin.Common.Tests.Paseto
{
    /// <summary>
    /// Tamper tests: flipping any single byte of the header, nonce, ciphertext, tag or
    /// footer must cause decryption to throw. Also covers wrong-key and wrong-footer
    /// rejection.
    /// </summary>
    public static class PasetoV4LocalTamperTests
    {
        private const string Payload = "tamper-me-this-is-a-secret-message-abcdef";

        private const string Footer = "{\"kid\":\"zVhMiPBP9fRf2snEcT7gFTioeA9COcNy9DfgL1W60haN\"}";

        private static readonly byte[] Key = { 0x70, 0x71, 0x72, 0x73, 0x74, 0x75, 0x76, 0x77, 0x78, 0x79, 0x7a, 0x7b, 0x7c, 0x7d, 0x7e, 0x7f, 0x80, 0x81, 0x82, 0x83, 0x84, 0x85, 0x86, 0x87, 0x88, 0x89, 0x8a, 0x8b, 0x8c, 0x8d, 0x8e, 0x8f };

        private static readonly byte[] Nonce = EnumerableRange32(0x5a);

        /// <summary>
        /// Flipping any of the 8 header characters must fail decryption.
        /// </summary>
        [Fact]
        public static void FlipEveryHeaderByte_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce);
            AssertAllMutationsFail(token, count: 9, mutate: i =>
            {
                char[] chars = token.ToCharArray();
                chars[i] = (char)(chars[i] ^ 0x01);
                return new string(chars);
            });
        }

        /// <summary>
        /// Flipping any of the 32 nonce bytes must fail decryption (MAC covers the nonce).
        /// </summary>
        [Fact]
        public static void FlipEveryNonceByte_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce);
            byte[] body = Base64Url.Decode(token[9..]);
            AssertAllMutationsFail(token, count: 32, mutate: i => RebuildTokenBody(token, body, i));
        }

        /// <summary>
        /// Flipping any ciphertext byte must fail decryption (MAC covers the ciphertext).
        /// </summary>
        [Fact]
        public static void FlipEveryCiphertextByte_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce);
            byte[] body = Base64Url.Decode(token[9..]);
            int ciphertextOffset = 32;
            int ciphertextLength = Encoding.UTF8.GetByteCount(Payload);
            AssertAllMutationsFail(token, count: ciphertextLength, mutate: i => RebuildTokenBody(token, body, ciphertextOffset + i));
        }

        /// <summary>
        /// Flipping any of the 32 tag bytes must fail decryption (MAC mismatch).
        /// </summary>
        [Fact]
        public static void FlipEveryTagByte_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce);
            byte[] body = Base64Url.Decode(token[9..]);
            int tagOffset = body.Length - 32;
            AssertAllMutationsFail(token, count: 32, mutate: i => RebuildTokenBody(token, body, tagOffset + i));
        }

        /// <summary>
        /// Flipping any footer byte (encoded segment) must fail decryption.
        /// </summary>
        [Fact]
        public static void FlipEveryFooterByte_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce, Footer);
            int separator = token.LastIndexOf('.');
            string body = token[9..separator];
            byte[] footerBytes = Base64Url.Decode(token[(separator + 1)..]);
            AssertAllMutationsFail(token, count: footerBytes.Length, mutate: i =>
            {
                byte[] mutated = (byte[])footerBytes.Clone();
                mutated[i] ^= 0x01;
                return body + "." + Base64Url.Encode(mutated);
            });
        }

        /// <summary>
        /// Editing the plaintext footer must fail decryption (footer is authenticated).
        /// </summary>
        [Fact]
        public static void AlteredFooterText_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce, Footer);
            int separator = token.LastIndexOf('.');
            string body = token[..separator];
            string mutatedFooter = Footer.Replace("kid", "xid", StringComparison.Ordinal);
            string mutatedToken = body + "." + Base64Url.Encode(Encoding.UTF8.GetBytes(mutatedFooter));
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, mutatedToken));
        }

        /// <summary>
        /// Decrypting with a different key must fail, and a wrong expectedFooter must fail.
        /// </summary>
        [Fact]
        public static void WrongKeyOrWrongExpectedFooter_Throws()
        {
            var token = PasetoV4Local.EncryptWithNonce(Key, Payload, Nonce, Footer);

            byte[] wrongKey = (byte[])Key.Clone();
            wrongKey[0] ^= 0x01;
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(wrongKey, token, Footer));

            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(Key, token, "a-different-footer"));
        }

        private static string RebuildTokenBody(string token, byte[] body, int index)
        {
            byte[] mutated = (byte[])body.Clone();
            mutated[index] ^= 0x01;
            return token[..9] + Base64Url.Encode(mutated);
        }

        private static void AssertAllMutationsFail(string token, int count, Func<int, string> mutate)
        {
            var failures = new List<string>();
            for (int i = 0; i < count; i++)
            {
                string candidate = mutate(i);
                try
                {
                    PasetoV4Local.Decrypt(Key, candidate);
                    failures.Add(i.ToString(CultureInfo.InvariantCulture));
                }
                catch (PasetoException)
                {
                    // Expected.
                }
            }

            Assert.True(failures.Count == 0, "Mutations that did NOT throw: " + string.Join(", ", failures));
        }

        private static byte[] EnumerableRange32(byte start)
        {
            var result = new byte[32];
            for (int i = 0; i < 32; i++)
            {
                result[i] = (byte)(start + i);
            }

            return result;
        }
    }
}
