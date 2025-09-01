using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;
using Jellyfin.Data;
using Jellyfin.Data.Events.Users;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Jellyfin.Database.Implementations.Enums;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Events;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OtpNet;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// Default Jellyfin password-based (and optional TOTP) authentication provider.
    /// </summary>
    public class DefaultAuthenticationProvider(
        IDbContextFactory<JellyfinDbContext> contextFactory,
        ILogger<DefaultAuthenticationProvider> logger,
        ICryptoProvider cryptographyProvider,
        IUserManager userManager,
        IEventManager eventManager)
        : AbstractAuthenticationProvider<UsernamePasswordAuthData, NoData, PasswordUserData>(contextFactory, userManager),
        IPasswordChangeable
    {
        /// <inheritdoc />
        public override string Name => "UsernamePassword";

        /// <inheritdoc/>
        public override async Task<AuthenticationResult> Authenticate(UsernamePasswordAuthData authenticationData)
        {
            ArgumentNullException.ThrowIfNull(authenticationData.Username);
            ArgumentNullException.ThrowIfNull(authenticationData.Password);

            var user = UserManager.GetUserByName(authenticationData.Username);

            if (user is null)
            {
                return AuthenticationResult.AnonymousFailure();
            }

            var userData = await GetUserData(user).ConfigureAwait(false);

            // As long as jellyfin supports password-less users, we need this little block here to accommodate
            if (string.IsNullOrEmpty(userData.PasswordHash) && string.IsNullOrEmpty(authenticationData.Password))
            {
                return AuthenticationResult.Success(user);
            }

            // Handle the case when the stored password is null, but the user tried to login with a password
            if (string.IsNullOrEmpty(userData.PasswordHash))
            {
                return AuthenticationResult.Failure(user);
            }

            PasswordHash readyHash = PasswordHash.Parse(userData.PasswordHash);
            if (!cryptographyProvider.Verify(readyHash, authenticationData.Password))
            {
                return AuthenticationResult.Failure(user);
            }

            // Migrate old hashes to the new default
            if (!string.Equals(readyHash.Id, cryptographyProvider.DefaultHashMethod, StringComparison.Ordinal)
                || int.Parse(readyHash.Parameters["iterations"], CultureInfo.InvariantCulture) != Constants.DefaultIterations)
            {
                logger.LogInformation("Migrating password hash of {User} to the latest default", user.Username);
                await ChangePassword(user, authenticationData.Password).ConfigureAwait(false);
            }

            if (!userData.MfaEnabled) // no mfa, done here
            {
                return AuthenticationResult.Success(user);
            }

            if (userData.TOTPSecret is null) // require MFA setup
            {
                return AuthenticationResult.Failure(user, 1301);
            }

            if (authenticationData.TOTP is null)
            {
                return AuthenticationResult.Failure(user, 1300); // incorrect or missing TOTP
            }

            var totp = new Totp(userData.TOTPSecret);
            if (!totp.VerifyTotp(authenticationData.TOTP, out long timeStepMatched, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                return AuthenticationResult.Failure(user, 1300); // incorrect or missing TOTP
            }

            return AuthenticationResult.Success(user);
        }

        /// <inheritdoc/>
        public override Task<NoData> InitialGlobalData()
        {
            return Task.FromResult(default(NoData));
        }

        /// <inheritdoc/>
        public override Task<PasswordUserData> InitialUserData()
        {
            return Task.FromResult(new PasswordUserData()
            {
                MfaEnabled = false,
                PasswordHash = null
            });
        }

        /// <inheritdoc/>
        public async Task ChangePassword(User user, string newPassword)
        {
            ArgumentNullException.ThrowIfNull(user);
            if (user.HasPermission(PermissionKind.IsAdministrator) && string.IsNullOrWhiteSpace(newPassword))
            {
                throw new ArgumentException("Admin user passwords must not be empty", nameof(newPassword));
            }

            if (string.IsNullOrEmpty(newPassword))
            {
                await SaveUserData(user, data => new PasswordUserData()
                {
                    PasswordHash = null,
                    MfaEnabled = data.MfaEnabled
                }).ConfigureAwait(false);
                return;
            }

            PasswordHash newPasswordHash = cryptographyProvider.CreatePasswordHash(newPassword);
            await SaveUserData(user, data => new PasswordUserData()
            {
                PasswordHash = newPasswordHash.ToString(),
                MfaEnabled = data.MfaEnabled
            }).ConfigureAwait(false);

            await eventManager.PublishAsync(new UserPasswordChangedEventArgs(user)).ConfigureAwait(false);
        }
    }

#pragma warning disable SA1201, CA1819 // Elements should appear in the correct order, Properties should not return arrays
    /// <summary>
    /// User data for the username password authentication provider.
    /// </summary>
    /// <param name="PasswordHash">Hashed user password.</param>
    /// <param name="MfaEnabled">Whether or not MFA is enabled.</param>
    /// <param name="TOTPSecret">The TOTP secret for this user.</param>
    public record struct PasswordUserData(string? PasswordHash, bool MfaEnabled, byte[]? TOTPSecret);
#pragma warning restore SA1201, CA1819 // Elements should appear in the correct order, Properties should not return arrays
}
