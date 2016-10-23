using System;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Model.Cryptography;

namespace MediaBrowser.Common.Implementations.Cryptography
{
    public class CryptographyProvider : ICryptographyProvider
    {
        public Guid GetMD5(string str)
        {
            using (var provider = MD5.Create())
            {
                return new Guid(provider.ComputeHash(Encoding.Unicode.GetBytes(str)));
            }
        }
    }
}
