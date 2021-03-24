using System;
using System.Collections.Generic;
using MediaBrowser.Common.Cryptography;
using Xunit;

namespace Jellyfin.Common.Tests.Cryptography
{
    public static class PasswordHashTests
    {
        [Fact]
        public static void Ctor_Null_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new PasswordHash(null!, Array.Empty<byte>()));
        }

        [Fact]
        public static void Ctor_Empty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => new PasswordHash(string.Empty, Array.Empty<byte>()));
        }

        public static IEnumerable<object[]> Parse_Valid_TestData()
        {
            // Id
            yield return new object[]
            {
                "$PBKDF2",
                new PasswordHash("PBKDF2", Array.Empty<byte>())
            };

            // Id + parameter
            yield return new object[]
            {
                "$PBKDF2$iterations=1000",
                new PasswordHash(
                    "PBKDF2",
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new Dictionary<string, string>()
                    {
                        { "iterations", "1000" },
                    })
            };

            // Id + parameters
            yield return new object[]
            {
                "$PBKDF2$iterations=1000,m=120",
                new PasswordHash(
                    "PBKDF2",
                    Array.Empty<byte>(),
                    Array.Empty<byte>(),
                    new Dictionary<string, string>()
                    {
                        { "iterations", "1000" },
                        { "m", "120" }
                    })
            };

            // Id + hash
            yield return new object[]
            {
                "$PBKDF2$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
                new PasswordHash(
                    "PBKDF2",
                    Convert.FromHexString("62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D"),
                    Array.Empty<byte>(),
                    new Dictionary<string, string>())
            };

            // Id + salt + hash
            yield return new object[]
            {
                "$PBKDF2$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
                new PasswordHash(
                    "PBKDF2",
                    Convert.FromHexString("62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D"),
                    Convert.FromHexString("69F420"),
                    new Dictionary<string, string>())
            };

            // Id + parameter + hash
            yield return new object[]
            {
                "$PBKDF2$iterations=1000$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
                new PasswordHash(
                    "PBKDF2",
                    Convert.FromHexString("62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D"),
                    Array.Empty<byte>(),
                    new Dictionary<string, string>()
                    {
                        { "iterations", "1000" }
                    })
            };

            // Id + parameters + hash
            yield return new object[]
            {
                "$PBKDF2$iterations=1000,m=120$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
                new PasswordHash(
                    "PBKDF2",
                    Convert.FromHexString("62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D"),
                    Array.Empty<byte>(),
                    new Dictionary<string, string>()
                    {
                        { "iterations", "1000" },
                        { "m", "120" }
                    })
            };

            // Id + parameters + salt + hash
            yield return new object[]
            {
                "$PBKDF2$iterations=1000,m=120$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D",
                new PasswordHash(
                    "PBKDF2",
                    Convert.FromHexString("62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D"),
                    Convert.FromHexString("69F420"),
                    new Dictionary<string, string>()
                    {
                        { "iterations", "1000" },
                        { "m", "120" }
                    })
            };
        }

        [Theory]
        [MemberData(nameof(Parse_Valid_TestData))]
        public static void Parse_Valid_Success(string passwordHashString, PasswordHash expected)
        {
            var passwordHash = PasswordHash.Parse(passwordHashString);
            Assert.Equal(expected.Id, passwordHash.Id);
            Assert.Equal(expected.Parameters, passwordHash.Parameters);
            Assert.Equal(expected.Salt.ToArray(), passwordHash.Salt.ToArray());
            Assert.Equal(expected.Hash.ToArray(), passwordHash.Hash.ToArray());
            Assert.Equal(expected.ToString(), passwordHash.ToString());
        }

        [Theory]
        [InlineData("$PBKDF2")]
        [InlineData("$PBKDF2$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        [InlineData("$PBKDF2$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        [InlineData("$PBKDF2$iterations=1000$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        [InlineData("$PBKDF2$iterations=1000,m=120$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        [InlineData("$PBKDF2$iterations=1000,m=120$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")]
        [InlineData("$PBKDF2$iterations=1000,m=120")]
        public static void ToString_Roundtrip_Success(string passwordHash)
        {
            Assert.Equal(passwordHash, PasswordHash.Parse(passwordHash).ToString());
        }

        [Fact]
        public static void Parse_Null_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PasswordHash.Parse(null));
        }

        [Fact]
        public static void Parse_Empty_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() => PasswordHash.Parse(string.Empty));
        }

        [Theory]
        [InlineData("$")] // No id
        [InlineData("$$")] // Empty segments
        [InlineData("PBKDF2$")] // Doesn't start with $
        [InlineData("$PBKDF2$$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Empty segment
        [InlineData("$PBKDF2$iterations=1000$$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Empty salt segment
        [InlineData("$PBKDF2$iterations=1000$69F420$")] // Empty hash segment
        [InlineData("$PBKDF2$=$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Invalid parmeter
        [InlineData("$PBKDF2$=1000$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Invalid parmeter
        [InlineData("$PBKDF2$iterations=$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Invalid parmeter
        [InlineData("$PBKDF2$iterations=$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D$")] // Ends on $
        [InlineData("$PBKDF2$iterations=$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D$")] // Extra segment
        [InlineData("$PBKDF2$iterations=$69F420$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D$anotherone")] // Extra segment
        [InlineData("$PBKDF2$iterations=$invalidstalt$62FBA410AFCA5B4475F35137AB2E8596B127E4D927BA23F6CC05C067E897042D")] // Invalid salt
        [InlineData("$PBKDF2$iterations=$69F420$invalid hash")] // Invalid hash
        [InlineData("$PBKDF2$69F420$")] // Empty hash
        public static void Parse_InvalidFormat_ThrowsFormatException(string passwordHash)
        {
            Assert.Throws<FormatException>(() => PasswordHash.Parse(passwordHash));
        }
    }
}
