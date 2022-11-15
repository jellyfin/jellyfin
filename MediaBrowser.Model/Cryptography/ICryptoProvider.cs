#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        string DefaultHashMethod { get; }

        /// <summary>
        /// Creates a new <see cref="PasswordHash" /> instance.
        /// </summary>
        /// <param name="password">The password that will be hashed.</param>
        /// <returns>A <see cref="PasswordHash" /> instance with the hash method, hash, salt and number of iterations.</returns>
        PasswordHash CreatePasswordHash(ReadOnlySpan<char> password);

        bool Verify(PasswordHash hash, ReadOnlySpan<char> password);

        byte[] GenerateSalt();

        byte[] GenerateSalt(int length);
    }
}
