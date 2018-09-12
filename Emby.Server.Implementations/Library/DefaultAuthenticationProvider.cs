using System;
using System.Collections.Generic;
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

        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }

        public Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser)
        {
            if (resolvedUser == null)
            {
                throw new Exception("Invalid username or password");
            }

            var success = string.Equals(GetPasswordHash(resolvedUser), GetHashedString(resolvedUser, password), StringComparison.OrdinalIgnoreCase);

            if (!success)
            {
                throw new Exception("Invalid username or password");
            }

            return Task.FromResult(new ProviderAuthenticationResult
            {
                Username = username
            });
        }

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
            string newPasswordHash = null;

            if (newPassword != null)
            {
                newPasswordHash = GetHashedString(user, newPassword);
            }

            if (string.IsNullOrWhiteSpace(newPasswordHash))
            {
                throw new ArgumentNullException("newPasswordHash");
            }

            user.Password = newPasswordHash;

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

        /// <summary>
        /// Gets the hashed string.
        /// </summary>
        public string GetHashedString(User user, string str)
        {
            var salt = user.Salt;
            if (salt != null)
            {
                // return BCrypt.HashPassword(str, salt);
            }

            // legacy
            return BitConverter.ToString(_cryptographyProvider.ComputeSHA1(Encoding.UTF8.GetBytes(str))).Replace("-", string.Empty);
        }
    }
}
