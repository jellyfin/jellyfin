using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Cryptography;

namespace Emby.Server.Implementations.Library
{
    public class DefaultAuthenticationProvider : IAuthenticationProvider, IRequiresResolvedUser
    {
        private readonly ICryptoProvider _cryptographyProvider;
        public DefaultAuthenticationProvider(ICryptoProvider crypto)
        {
            _cryptographyProvider = crypto;
        }

        public string Name => "Default";

        public bool IsEnabled => true;


        //This is dumb and an artifact of the backwards way auth providers were designed.
        //This version of authenticate was never meant to be called, but needs to be here for interface compat
        //Only the providers that don't provide local user support use this
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }


        //This is the verson that we need to use for local users. Because reasons.
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser)
        {
            ConvertPasswordFormat(resolvedUser);
            byte[] passwordbytes = Encoding.UTF8.GetBytes(password);
            bool success = false;
            if (resolvedUser == null)
            {
                success = false;
                throw new Exception("Invalid username or password");
            }
            if (!resolvedUser.Password.Contains("$"))
            {
                ConvertPasswordFormat(resolvedUser);
            }
            PasswordHash ReadyHash = new PasswordHash(resolvedUser.Password);
            byte[] CalculatedHash;
            string CalculatedHashString;
            if (_cryptographyProvider.GetSupportedHashMethods().Any(i => i == ReadyHash.Id))
            {
                if (String.IsNullOrEmpty(ReadyHash.Salt))
                {
                    CalculatedHash = _cryptographyProvider.ComputeHash(ReadyHash.Id, passwordbytes);
                    CalculatedHashString = BitConverter.ToString(CalculatedHash).Replace("-", string.Empty);
                }
                else
                {
                    CalculatedHash = _cryptographyProvider.ComputeHash(ReadyHash.Id, passwordbytes, ReadyHash.SaltBytes);
                    CalculatedHashString = BitConverter.ToString(CalculatedHash).Replace("-", string.Empty);
                }
                if (CalculatedHashString == ReadyHash.Hash)
                {
                    success = true;
                    //throw new Exception("Invalid username or password");
                }
            }
            else
            {
                success = false;
                throw new Exception(String.Format("Requested crypto method not available in provider: {0}", ReadyHash.Id));
            }

            //var success = string.Equals(GetPasswordHash(resolvedUser), GetHashedString(resolvedUser, password), StringComparison.OrdinalIgnoreCase);

            if (!success)
            {
                throw new Exception("Invalid username or password");
            }

            return Task.FromResult(new ProviderAuthenticationResult
            {
                Username = username
            });
        }

        //This allows us to move passwords forward to the newformat without breaking. They are still insecure, unsalted, and dumb before a password change
        //but at least they are in the new format.
        private void ConvertPasswordFormat(User user)
        {
            if (!string.IsNullOrEmpty(user.Password))
            {
                if (!user.Password.Contains("$"))
                {
                    string hash = user.Password;
                    user.Password = String.Format("$SHA1${0}", hash);
                }
                if (user.EasyPassword != null && !user.EasyPassword.Contains("$"))
                {
                    string hash = user.EasyPassword;
                    user.EasyPassword = String.Format("$SHA1${0}", hash);
                }
            }
        }

        // OLD VERSION //public Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser)
        // OLD VERSION //{
        // OLD VERSION //    if (resolvedUser == null)
        // OLD VERSION //    {
        // OLD VERSION //        throw new Exception("Invalid username or password");
        // OLD VERSION //    }
        // OLD VERSION //
        // OLD VERSION //    var success = string.Equals(GetPasswordHash(resolvedUser), GetHashedString(resolvedUser, password), StringComparison.OrdinalIgnoreCase);
        // OLD VERSION //
        // OLD VERSION //    if (!success)
        // OLD VERSION //    {
        // OLD VERSION //        throw new Exception("Invalid username or password");
        // OLD VERSION //    }
        // OLD VERSION //
        // OLD VERSION //    return Task.FromResult(new ProviderAuthenticationResult
        // OLD VERSION //    {
        // OLD VERSION //        Username = username
        // OLD VERSION //    });
        // OLD VERSION //}

        public Task<bool> HasPassword(User user)
        {
            var hasConfiguredPassword = !IsPasswordEmpty(user, GetPasswordHash(user));
            return Task.FromResult(hasConfiguredPassword);
        }

        private bool IsPasswordEmpty(User user, string passwordHash)
        {
            return string.Equals(passwordHash, GetEmptyHashedString(user), StringComparison.OrdinalIgnoreCase);
        }

        public Task ChangePassword(User user, string newPassword)
        {
            //string newPasswordHash = null;
            ConvertPasswordFormat(user);
            PasswordHash passwordHash = new PasswordHash(user.Password);
            if(passwordHash.Id == "SHA1" && string.IsNullOrEmpty(passwordHash.Salt))
            {
                passwordHash.SaltBytes = _cryptographyProvider.GenerateSalt();
                passwordHash.Salt = BitConverter.ToString(passwordHash.SaltBytes).Replace("-","");
                passwordHash.Id = _cryptographyProvider.DefaultHashMethod;
                passwordHash.Hash = GetHashedStringChangeAuth(newPassword, passwordHash);
            }else if (newPassword != null)
            {
                passwordHash.Hash = GetHashedString(user, newPassword);
            }

            if (string.IsNullOrWhiteSpace(passwordHash.Hash))
            {
                throw new ArgumentNullException(nameof(passwordHash.Hash));
            }

            user.Password = passwordHash.ToString();

            return Task.CompletedTask;
        }

        public string GetPasswordHash(User user)
        {
            return string.IsNullOrEmpty(user.Password)
                ? GetEmptyHashedString(user)
                : user.Password;
        }

        public string GetEmptyHashedString(User user)
        {
            return GetHashedString(user, string.Empty);
        }

        public string GetHashedStringChangeAuth(string NewPassword, PasswordHash passwordHash)
        {
            return BitConverter.ToString(_cryptographyProvider.ComputeHash(passwordHash.Id, Encoding.UTF8.GetBytes(NewPassword), passwordHash.SaltBytes)).Replace("-", string.Empty);
        }

        /// <summary>
        /// Gets the hashed string.
        /// </summary>
        public string GetHashedString(User user, string str)
        {
            //This is legacy. Deprecated in the auth method.
            //return BitConverter.ToString(_cryptoProvider2.ComputeSHA1(Encoding.UTF8.GetBytes(str))).Replace("-", string.Empty);
            PasswordHash passwordHash;
            if (String.IsNullOrEmpty(user.Password))
            {
                passwordHash = new PasswordHash(_cryptographyProvider);
            }
            else
            {
                ConvertPasswordFormat(user);
                passwordHash = new PasswordHash(user.Password);
            }
            if (passwordHash.SaltBytes != null)
            {
                return BitConverter.ToString(_cryptographyProvider.ComputeHash(passwordHash.Id, Encoding.UTF8.GetBytes(str), passwordHash.SaltBytes)).Replace("-",string.Empty);
            }
            else
            {
                return BitConverter.ToString(_cryptographyProvider.ComputeHash(passwordHash.Id, Encoding.UTF8.GetBytes(str))).Replace("-", string.Empty);
                //throw new Exception("User does not have a hash, this should not be possible");
            }


        }
    }
}
