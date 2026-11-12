using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Jellyfin.Paseto;
using Xunit;

namespace Jellyfin.Common.Tests.Paseto
{
    /// <summary>
    /// Conformance tests against the official PASETO v4 test vectors
    /// (paseto-standard/test-vectors, v4.json, MIT).
    /// </summary>
    public static class PasetoV4LocalVectorTests
    {
        private static readonly byte[] FallbackKey = Convert.FromHexString("707172737475767778797a7b7c7d7e7f808182838485868788898a8b8c8d8e8f");

        /// <summary>
        /// All success (expect-fail=false) v4.local vectors.
        /// </summary>
        public static IEnumerable<object[]> SuccessVectorData() => LoadVectors()
            .Where(v => !v.ExpectFail && v.Token?.StartsWith("v4.local", StringComparison.Ordinal) == true)
            .Select(v => new object[] { v });

        /// <summary>
        /// All expect-fail vectors.
        /// </summary>
        public static IEnumerable<object[]> FailVectorData() => LoadVectors()
            .Where(v => v.ExpectFail)
            .Select(v => new object[] { v });

        /// <summary>
        /// Decrypting an official success vector with its fixed key must return the exact payload.
        /// </summary>
        [Theory]
        [MemberData(nameof(SuccessVectorData))]
        public static void Decrypt_SuccessVector_ReturnsPayload(PasetoV4Vector vector)
        {
            byte[] key = Convert.FromHexString(vector.Key!);
            string decrypted = PasetoV4Local.Decrypt(key, vector.Token!, vector.Footer, vector.ImplicitAssertion);
            Assert.Equal(vector.Payload, decrypted);
        }

        /// <summary>
        /// Encrypting an official success vector with its fixed key and nonce must reproduce the exact token.
        /// </summary>
        [Theory]
        [MemberData(nameof(SuccessVectorData))]
        public static void Encrypt_SuccessVector_FixedNonce_ReproducesExactToken(PasetoV4Vector vector)
        {
            byte[] key = Convert.FromHexString(vector.Key!);
            byte[] nonce = Convert.FromHexString(vector.Nonce!);
            string token = PasetoV4Local.EncryptWithNonce(key, vector.Payload!, nonce, vector.Footer, vector.ImplicitAssertion);
            Assert.Equal(vector.Token, token);
        }

        /// <summary>
        /// Every expect-fail vector must throw <see cref="PasetoException"/>.
        /// </summary>
        [Theory]
        [MemberData(nameof(FailVectorData))]
        public static void Decrypt_ExpectFailVector_Throws(PasetoV4Vector vector)
        {
            byte[] key = vector.Key is null ? FallbackKey : Convert.FromHexString(vector.Key);
            Assert.Throws<PasetoException>(() => PasetoV4Local.Decrypt(key, vector.Token!, vector.Footer, vector.ImplicitAssertion));
        }

        private static IReadOnlyList<PasetoV4Vector> LoadVectors()
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Paseto", "paseto-v4-test-vectors.json");
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(path));
            return document.RootElement.GetProperty("tests")
                .EnumerateArray()
                .Select(ParseVector)
                .ToArray();
        }

        private static PasetoV4Vector ParseVector(JsonElement element) => new PasetoV4Vector
        {
            Name = element.TryGetProperty("name", out JsonElement name) ? name.GetString()! : string.Empty,
            ExpectFail = element.TryGetProperty("expect-fail", out JsonElement expectFail) && expectFail.GetBoolean(),
            Key = element.TryGetProperty("key", out JsonElement key) ? key.GetString() : null,
            Nonce = element.TryGetProperty("nonce", out JsonElement nonce) ? nonce.GetString() : null,
            Payload = element.TryGetProperty("payload", out JsonElement payload) ? payload.GetString() : null,
            Footer = element.TryGetProperty("footer", out JsonElement footer) ? footer.GetString() : null,
            Token = element.TryGetProperty("token", out JsonElement token) ? token.GetString() : null,
            ImplicitAssertion = element.TryGetProperty("implicit-assertion", out JsonElement implicitAssertion) ? implicitAssertion.GetString() : null
        };

        /// <summary>
        /// One parsed entry of the official v4.json test vector file.
        /// </summary>
        public sealed class PasetoV4Vector
        {
            public string Name { get; set; } = string.Empty;

            public bool ExpectFail { get; set; }

            public string? Key { get; set; }

            public string? Nonce { get; set; }

            public string? Payload { get; set; }

            public string? Footer { get; set; }

            public string? Token { get; set; }

            public string? ImplicitAssertion { get; set; }

            public override string ToString() => Name;
        }
    }
}
