using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Linq;
using MediaBrowser.Model.Cryptography;

namespace Emby.Server.Implementations.Cryptography
{
    public class CryptographyProvider : ICryptoProvider
    {
        private HashSet<string> SupportedHashMethods;
        public string DefaultHashMethod => "PBKDF2";
        private RandomNumberGenerator rng;
        private int defaultiterations = 1000;
        public CryptographyProvider()
        {
            //FIXME: When we get DotNet Standard 2.1 we need to revisit how we do the crypto
            //Currently supported hash methods from https://docs.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptoconfig?view=netcore-2.1
            //there might be a better way to autogenerate this list as dotnet updates, but I couldn't find one
            //Please note the default method of PBKDF2 is not included, it cannot be used to generate hashes cleanly as it is actually a pbkdf with sha1
            SupportedHashMethods = new HashSet<string>()
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

        private byte[] PBKDF2(string method, byte[] bytes, byte[] salt, int iterations)
        {
            //downgrading for now as we need this library to be dotnetstandard compliant
            //with this downgrade we'll add a check to make sure we're on the downgrade method at the moment
            if(method == DefaultHashMethod)
            {
                using (var r = new Rfc2898DeriveBytes(bytes, salt, iterations))
                {
                    return r.GetBytes(32);
                }
            }
            throw new CryptographicException($"Cannot currently use PBKDF2 with requested hash method: {method}");
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
            if(HashMethod == DefaultHashMethod)
            {
                return PBKDF2(HashMethod, bytes, salt, defaultiterations);
            }
            else if (SupportedHashMethods.Contains(HashMethod))
            {
                using (var h = HashAlgorithm.Create(HashMethod))
                {
                    if (salt.Length == 0)
                    {
                        return h.ComputeHash(bytes);
                    }
                    else
                    {
                        return h.ComputeHash(bytes.Concat(salt).ToArray());
                    }
                }
            }
            else
            {
                throw new CryptographicException($"Requested hash method is not supported: {HashMethod}");
            }
        }

        public byte[] ComputeHashWithDefaultMethod(byte[] bytes, byte[] salt)
        {
            return PBKDF2(DefaultHashMethod, bytes, salt, defaultiterations);
        }
        
        public byte[] ComputeHash(PasswordHash hash)
        {
            int iterations = defaultiterations;
            if (!hash.Parameters.ContainsKey("iterations"))
            {
                hash.Parameters.Add("iterations", defaultiterations.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                try
                {
                    iterations = int.Parse(hash.Parameters["iterations"]);
                }
                catch (Exception e)
                {
                    throw new InvalidDataException($"Couldn't successfully parse iterations value from string: {hash.Parameters["iterations"]}", e);
                }
            }
            return PBKDF2(hash.Id, hash.HashBytes, hash.SaltBytes, iterations);
        }

        public byte[] GenerateSalt()
        {
            byte[] salt = new byte[64];
            rng.GetBytes(salt);
            return salt;
        }
    }
}
