#pragma warning disable CS1591

using System.Collections.Generic;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        string DefaultHashMethod { get; }

        IEnumerable<string> GetSupportedHashMethods();

        byte[] ComputeHash(string hashMethod, byte[] bytes, byte[] salt);

        byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt);

        byte[] GenerateSalt();

        byte[] GenerateSalt(int length);
    }
}
