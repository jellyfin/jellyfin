#pragma warning disable CS1591
#pragma warning disable SA1600

using System.Collections.Generic;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        string DefaultHashMethod { get; }

        IEnumerable<string> GetSupportedHashMethods();

        byte[] ComputeHash(string HashMethod, byte[] bytes, byte[] salt);

        byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt);

        byte[] GenerateSalt();

        byte[] GenerateSalt(int length);
    }
}
