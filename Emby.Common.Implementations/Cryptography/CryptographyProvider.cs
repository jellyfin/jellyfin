using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Model.Cryptography;

namespace Emby.Common.Implementations.Cryptography
{
    public class CryptographyProvider : ICryptographyProvider
    {
        public Guid GetMD5(string str)
        {
            return new Guid(GetMD5Bytes(str));
        }
        public byte[] GetMD5Bytes(string str)
        {
            using (var provider = MD5.Create())
            {
                return provider.ComputeHash(Encoding.Unicode.GetBytes(str));
            }
        }
        public byte[] GetMD5Bytes(Stream str)
        {
            using (var provider = MD5.Create())
            {
                return provider.ComputeHash(str);
            }
        }
    }
}
