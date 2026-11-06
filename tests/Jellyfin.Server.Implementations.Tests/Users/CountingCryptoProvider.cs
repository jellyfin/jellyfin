using System;
using MediaBrowser.Model.Cryptography;

namespace Jellyfin.Server.Implementations.Tests.Users
{
    /// <summary>
    /// An <see cref="ICryptoProvider"/> that records calls instead of doing real hashing.
    /// Moq can't match <see cref="ReadOnlySpan{T}"/> arguments, so the tests use this fake instead.
    /// </summary>
    internal sealed class CountingCryptoProvider : ICryptoProvider
    {
        public int CreateCalls { get; private set; }

        public int VerifyCalls { get; private set; }

        public PasswordHash? LastCreatedHash { get; private set; }

        public PasswordHash? LastVerifiedHash { get; private set; }

        public bool VerifyResult { get; set; }

        public string DefaultHashMethod => "PBKDF2-SHA512";

        public PasswordHash CreatePasswordHash(ReadOnlySpan<char> password)
        {
            CreateCalls++;
            LastCreatedHash = new PasswordHash(DefaultHashMethod, new byte[] { 1 }, new byte[] { 2 });
            return LastCreatedHash;
        }

        public bool Verify(PasswordHash hash, ReadOnlySpan<char> password)
        {
            VerifyCalls++;
            LastVerifiedHash = hash;
            return VerifyResult;
        }

        public byte[] GenerateSalt() => new byte[] { 2 };

        public byte[] GenerateSalt(int length) => new byte[length];
    }
}
