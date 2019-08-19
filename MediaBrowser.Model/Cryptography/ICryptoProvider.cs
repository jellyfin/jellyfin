using System;
using System.IO;
using System.Collections.Generic;

namespace MediaBrowser.Model.Cryptography
{
    public interface ICryptoProvider
    {
        string DefaultHashMethod { get; }
        [Obsolete("Use System.Security.Cryptography.MD5 directly")]
        Guid GetMD5(string str);
        [Obsolete("Use System.Security.Cryptography.MD5 directly")]
        byte[] ComputeMD5(Stream str);
        [Obsolete("Use System.Security.Cryptography.MD5 directly")]
        byte[] ComputeMD5(byte[] bytes);
        [Obsolete("Use System.Security.Cryptography.SHA1 directly")]
        byte[] ComputeSHA1(byte[] bytes);
        IEnumerable<string> GetSupportedHashMethods();
        byte[] ComputeHash(string HashMethod, byte[] bytes);
        byte[] ComputeHashWithDefaultMethod(byte[] bytes);
        byte[] ComputeHash(string HashMethod, byte[] bytes, byte[] salt);
        byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt);
        byte[] ComputeHash(PasswordHash hash);
        byte[] GenerateSalt();
    }
}
