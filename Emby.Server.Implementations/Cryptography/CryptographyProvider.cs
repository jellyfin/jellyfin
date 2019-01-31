using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using MediaBrowser.Model.Cryptography;

namespace Emby.Server.Implementations.Cryptography
{
    public class CryptographyProvider : ICryptoProvider
    {
        private List<string> SupportedHashMethods = new List<string>();
        private string DefaultHashMethod = "SHA256";
        private RandomNumberGenerator rng;
        private int defaultiterations = 1000;
        public CryptographyProvider()
        {
            //Currently supported hash methods from https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptoconfig?view=netcore-2.1
            //there might be a better way to autogenerate this list as dotnet updates, but I couldn't find one
            SupportedHashMethods = new List<string>
            {
               "MD5"
                ,"System.Security.Cryptography.MD5"
                ,"SHA"
                ,"SHA1"
                ,"System.Security.Cryptography.SHA1"
                ,"SHA256"
                ,"SHA-256"
                ,"System.Security.Cryptography.SHA256"
                ,"SHA384"
                ,"SHA-384"
                ,"System.Security.Cryptography.SHA384"
                ,"SHA512"
                ,"SHA-512"
                ,"System.Security.Cryptography.SHA512"
            };
            rng = RandomNumberGenerator.Create();
        }

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

        public IEnumerable<string> GetSupportedHashMethods()
        {
            return SupportedHashMethods;
        }

        private byte[] PBKDF2(string method, byte[] bytes, byte[] salt)
        {
            using (var r = new Rfc2898DeriveBytes(bytes, salt, defaultiterations, new HashAlgorithmName(method)))
            {
                return r.GetBytes(32);
            }
        }

        public byte[] ComputeHash(string HashMethod, byte[] bytes)
        {
            return ComputeHash(HashMethod, bytes, new byte[0]);
        }

        public byte[] ComputeHashWithDefaultMethod(byte[] bytes)
        {
            return ComputeHash(DefaultHashMethod, bytes);
        }

        public byte[] ComputeHash(string HashMethod, byte[] bytes, byte[] salt)
        {
            if (SupportedHashMethods.Contains(HashMethod))
            {
                if (salt.Length == 0)
                {
                    using (var h = HashAlgorithm.Create(HashMethod))
                    {
                        return h.ComputeHash(bytes);
                    }
                }
                else
                {
                    return PBKDF2(HashMethod, bytes, salt);
                }
            }
            else
            {
                throw new CryptographicException(String.Format("Requested hash method is not supported: {0}", HashMethod));
            }
        }

        public byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt)
        {
            return PBKDF2(DefaultHashMethod, bytes, salt);
        }
        
        public byte[] ComputeHash(PasswordHash hash)
        {
            return ComputeHash(hash.Id, hash.HashBytes, hash.SaltBytes);
        }
        
        public byte[] GenerateSalt()
        {
            byte[] salt = new byte[8];
            rng.GetBytes(salt);
            return salt;
        }
    }
}
