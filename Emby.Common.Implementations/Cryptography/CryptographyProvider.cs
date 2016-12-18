using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Model.Cryptography;

namespace Emby.Common.Implementations.Cryptography
{
    public class CryptographyProvider : ICryptoProvider
    {
        public Guid GetMD5(string str)
        {
            return new Guid(ComputeMD5(Encoding.Unicode.GetBytes(str)));
        }

        public byte[] ComputeSHA1(byte[] bytes)
        {
            using (var provider = SHA1.Create())
            {
                return provider.ComputeHash(bytes);
            }
        }

        public byte[] ComputeMD5(Stream str)
        {
            using (var provider = MD5.Create())
            {
                return provider.ComputeHash(str);
            }
        }

        public byte[] ComputeMD5(byte[] bytes)
        {
            using (var provider = MD5.Create())
            {
                return provider.ComputeHash(bytes);
            }
        }
    }
}
