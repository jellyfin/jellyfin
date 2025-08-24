using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Model.Cryptography;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// The default authentication provider.
    /// </summary>
    public class DefaultAuthenticationProvider : IAuthenticationProvider<string>
    {
        private readonly ILogger<DefaultAuthenticationProvider> _logger;
        private readonly ICryptoProvider _cryptographyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="cryptographyProvider">The cryptography provider.</param>
        public DefaultAuthenticationProvider(ILogger<DefaultAuthenticationProvider> logger, ICryptoProvider cryptographyProvider)
        {
            _logger = logger;
            _cryptographyProvider = cryptographyProvider;
        }

        /// <inheritdoc />
        public string Name => "UsernamePassword";

        private bool HasPassword(User user)
            => !string.IsNullOrEmpty(user?.Password);

        private Task ChangePassword(User user, string newPassword)
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

        /// <inheritdoc/>
        public Task<User?> Authenticate(User? user, string password)
        {
            [DoesNotReturn]
            static void ThrowAuthenticationException()
            {
                throw new AuthenticationException("Invalid username or password");
            }

            if (user is null)
            {
                ThrowAuthenticationException();
            }

            // As long as jellyfin supports password-less users, we need this little block here to accommodate
            if (!HasPassword(user) && string.IsNullOrEmpty(password))
            {
                return Task.FromResult<User?>(user);
            }

            // Handle the case when the stored password is null, but the user tried to login with a password
            if (user.Password is null)
            {
                ThrowAuthenticationException();
            }

            PasswordHash readyHash = PasswordHash.Parse(user.Password);
            if (!_cryptographyProvider.Verify(readyHash, password))
            {
                ThrowAuthenticationException();
            }

            // Migrate old hashes to the new default
            if (!string.Equals(readyHash.Id, _cryptographyProvider.DefaultHashMethod, StringComparison.Ordinal)
                || int.Parse(readyHash.Parameters["iterations"], CultureInfo.InvariantCulture) != Constants.DefaultIterations)
            {
                _logger.LogInformation("Migrating password hash of {User} to the latest default", user.Username);
                ChangePassword(user, password);
            }

            return Task.FromResult<User?(user);
        }
    }
}
