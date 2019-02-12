using System;
using System.IO;
using System.Collections.Generic;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        Guid GetMD5(string str);
        byte[] ComputeMD5(Stream str);
        byte[] ComputeMD5(byte[] bytes);
        byte[] ComputeSHA1(byte[] bytes);
        IEnumerable<string> GetSupportedHashMethods();
        byte[] ComputeHash(string HashMethod, byte[] bytes);
        byte[] ComputeHashWithDefaultMethod(byte[] bytes);
        byte[] ComputeHash(string HashMethod, byte[] bytes, byte[] salt);
        byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt);
        byte[] ComputeHash(PasswordHash hash);
        byte[] GenerateSalt();
        string DefaultHashMethod { get; }
    }
}
