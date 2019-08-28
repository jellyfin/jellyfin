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
        public DefaultAuthenticationProvider(ICryptoProvider cryptographyProvider)
        {
            _cryptographyProvider = cryptographyProvider;
        }

        public string Name => "Default";

        public bool IsEnabled => true;

        // This is dumb and an artifact of the backwards way auth providers were designed.
        // This version of authenticate was never meant to be called, but needs to be here for interface compat
        // Only the providers that don't provide local user support use this
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }

        // This is the version that we need to use for local users. Because reasons.
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser)
        {
            bool success = false;
            if (resolvedUser == null)
            {
                throw new ArgumentNullException(nameof(resolvedUser));
            }

            // As long as jellyfin supports passwordless users, we need this little block here to accommodate
            if (!HasPassword(resolvedUser) && string.IsNullOrEmpty(password))
            {
                return Task.FromResult(new ProviderAuthenticationResult
                {
                    Username = username
                });
            }

            ConvertPasswordFormat(resolvedUser);
            byte[] passwordbytes = Encoding.UTF8.GetBytes(password);

            PasswordHash readyHash = new PasswordHash(resolvedUser.Password);
            if (_cryptographyProvider.GetSupportedHashMethods().Contains(readyHash.Id)
                || _cryptographyProvider.DefaultHashMethod == readyHash.Id)
            {
                byte[] calculatedHash = _cryptographyProvider.ComputeHash(readyHash.Id, passwordbytes, readyHash.Salt);

                if (calculatedHash.SequenceEqual(readyHash.Hash))
                {
                    success = true;
                }
            }
            else
            {
                throw new AuthenticationException($"Requested crypto method not available in provider: {readyHash.Id}");
            }

            if (!success)
            {
                throw new AuthenticationException("Invalid username or password");
            }

            return Task.FromResult(new ProviderAuthenticationResult
            {
                Username = username
            });
        }

        // This allows us to move passwords forward to the newformat without breaking. They are still insecure, unsalted, and dumb before a password change
        // but at least they are in the new format.
        private void ConvertPasswordFormat(User user)
        {
            if (string.IsNullOrEmpty(user.Password))
            {
                return;
            }

            if (user.Password.IndexOf('$') == -1)
            {
                string hash = user.Password;
                user.Password = string.Format("$SHA1${0}", hash);
            }

            if (user.EasyPassword != null
                && user.EasyPassword.IndexOf('$') == -1)
            {
                string hash = user.EasyPassword;
                user.EasyPassword = string.Format("$SHA1${0}", hash);
            }
        }

        public bool HasPassword(User user)
            => !string.IsNullOrEmpty(user.Password);

        public Task ChangePassword(User user, string newPassword)
        {
            ConvertPasswordFormat(user);

            // This is needed to support changing a no password user to a password user
            if (string.IsNullOrEmpty(user.Password))
            {
                PasswordHash newPasswordHash = new PasswordHash(_cryptographyProvider);
                newPasswordHash.Salt = _cryptographyProvider.GenerateSalt();
                newPasswordHash.Id = _cryptographyProvider.DefaultHashMethod;
                newPasswordHash.Hash = GetHashedChangeAuth(newPassword, newPasswordHash);
                user.Password = newPasswordHash.ToString();
                return Task.CompletedTask;
            }

            PasswordHash passwordHash = new PasswordHash(user.Password);
            if (passwordHash.Id == "SHA1"
                && passwordHash.Salt.Length == 0)
            {
                passwordHash.Salt = _cryptographyProvider.GenerateSalt();
                passwordHash.Id = _cryptographyProvider.DefaultHashMethod;
                passwordHash.Hash = GetHashedChangeAuth(newPassword, passwordHash);
            }
            else if (newPassword != null)
            {
                passwordHash.Hash = GetHashed(user, newPassword);
            }

            user.Password = passwordHash.ToString();

            return Task.CompletedTask;
        }

        public void ChangeEasyPassword(User user, string newPassword, string newPasswordHash)
        {
            ConvertPasswordFormat(user);

            if (newPassword != null)
            {
                newPasswordHash = string.Format("$SHA1${0}", GetHashedString(user, newPassword));
            }

            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentNullException(nameof(newPasswordHash));
            }

            user.EasyPassword = newPasswordHash;
        }

        public string GetEasyPasswordHash(User user)
        {
            // This should be removed in the future. This was added to let user login after
            // Jellyfin 10.3.3 failed to save a well formatted PIN.
            ConvertPasswordFormat(user);

            return string.IsNullOrEmpty(user.EasyPassword)
                ? null
                : PasswordHash.ConvertToByteString(new PasswordHash(user.EasyPassword).Hash);
        }

        internal byte[] GetHashedChangeAuth(string newPassword, PasswordHash passwordHash)
        {
            passwordHash.Hash = Encoding.UTF8.GetBytes(newPassword);
            return _cryptographyProvider.ComputeHash(passwordHash);
        }

        /// <summary>
        /// Gets the hashed string.
        /// </summary>
        public string GetHashedString(User user, string str)
        {
            PasswordHash passwordHash;
            if (string.IsNullOrEmpty(user.Password))
            {
                passwordHash = new PasswordHash(_cryptographyProvider);
            }
            else
            {
                ConvertPasswordFormat(user);
                passwordHash = new PasswordHash(user.Password);
            }

            if (passwordHash.Salt != null)
            {
                // the password is modern format with PBKDF and we should take advantage of that
                passwordHash.Hash = Encoding.UTF8.GetBytes(str);
                return PasswordHash.ConvertToByteString(_cryptographyProvider.ComputeHash(passwordHash));
            }
            else
            {
                // the password has no salt and should be called with the older method for safety
                return PasswordHash.ConvertToByteString(_cryptographyProvider.ComputeHash(passwordHash.Id, Encoding.UTF8.GetBytes(str)));
            }
        }

        public byte[] GetHashed(User user, string str)
        {
            PasswordHash passwordHash;
            if (string.IsNullOrEmpty(user.Password))
            {
                passwordHash = new PasswordHash(_cryptographyProvider);
            }
            else
            {
                ConvertPasswordFormat(user);
                passwordHash = new PasswordHash(user.Password);
            }

            if (passwordHash.Salt != null)
            {
                // the password is modern format with PBKDF and we should take advantage of that
                passwordHash.Hash = Encoding.UTF8.GetBytes(str);
                return _cryptographyProvider.ComputeHash(passwordHash);
            }
            else
            {
                // the password has no salt and should be called with the older method for safety
                return _cryptographyProvider.ComputeHash(passwordHash.Id, Encoding.UTF8.GetBytes(str));
            }
        }
    }
}
