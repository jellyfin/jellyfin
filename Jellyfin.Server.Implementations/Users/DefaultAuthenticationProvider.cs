using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Cryptography;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// The default authentication provider.
    /// </summary>
    public class DefaultAuthenticationProvider : IAuthenticationProvider, IRequiresResolvedUser
    {
        private readonly ICryptoProvider _cryptographyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="cryptographyProvider">The cryptography provider.</param>
        public DefaultAuthenticationProvider(ICryptoProvider cryptographyProvider)
        {
            _cryptographyProvider = cryptographyProvider;
        }

        /// <inheritdoc />
        public string Name => "Default";

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        // This is dumb and an artifact of the backwards way auth providers were designed.
        // This version of authenticate was never meant to be called, but needs to be here for interface compat
        // Only the providers that don't provide local user support use this
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        // This is the version that we need to use for local users. Because reasons.
        public Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser)
        {
            if (resolvedUser == null)
            {
                throw new AuthenticationException("Specified user does not exist.");
            }

            bool success = false;

            // As long as jellyfin supports password-less users, we need this little block here to accommodate
            if (!HasPassword(resolvedUser) && string.IsNullOrEmpty(password))
            {
                return Task.FromResult(new ProviderAuthenticationResult
                {
                    Username = username
                });
            }

            // Handle the case when the stored password is null, but the user tried to login with a password
            if (resolvedUser.Password == null)
            {
                throw new AuthenticationException("Invalid username or password");
            }

            PasswordHash readyHash = PasswordHash.Parse(resolvedUser.Password);
            success = _cryptographyProvider.Verify(readyHash, password);

            if (!success)
            {
                throw new AuthenticationException("Invalid username or password");
            }

            // Migrate old hashes to the new default
            if (!string.Equals(readyHash.Id, _cryptographyProvider.DefaultHashMethod, StringComparison.Ordinal))
            {
                ChangePassword(resolvedUser, password);
            }

            return Task.FromResult(new ProviderAuthenticationResult
            {
                Username = username
            });
        }

        /// <inheritdoc />
        public bool HasPassword(User user)
            => !string.IsNullOrEmpty(user?.Password);

        /// <inheritdoc />
        public Task ChangePassword(User user, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                user.Password = null;
                return Task.CompletedTask;
            }

            PasswordHash newPasswordHash = _cryptographyProvider.CreatePasswordHash(newPassword);
            user.Password = newPasswordHash.ToString();

            return Task.CompletedTask;
        }
    }
}
