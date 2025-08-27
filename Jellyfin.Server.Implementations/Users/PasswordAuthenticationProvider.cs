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
    /// Default Jellyfin password-based authentication provider.
    /// </summary>
    public class PasswordAuthenticationProvider
        : AbstractAuthenticationProvider<string>
    {
        private readonly ILogger<PasswordAuthenticationProvider> _logger;
        private readonly ICryptoProvider _cryptographyProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="PasswordAuthenticationProvider"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="cryptographyProvider">The cryptography provider.</param>
        public PasswordAuthenticationProvider(ILogger<PasswordAuthenticationProvider> logger, ICryptoProvider cryptographyProvider)
        {
            _logger = logger;
            _cryptographyProvider = cryptographyProvider;
        }

        /// <inheritdoc />
        public override string Name => "UsernamePassword";

        private Task ChangePasswordInternal(User user, string newPassword)
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
                ChangePasswordInternal(user, password);
            }

            return Task.FromResult<User?(user);
        }

        public override Task<User?> Authenticate(User? user, dynamic? authenticationData)
        {
            throw new NotImplementedException();
        }
    }
}
