using System;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Runtime.Intrinsics.X86;
using System.Security.Cryptography;
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
        : AbstractAuthenticationProvider<UsernamePasswordAuthData, NoData, DefaultAuthProviderUserData>(contextFactory, userManager),
        IPasswordChangeable
    {
        private const int TOTPCodeLength = 6;

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

            var maybeUserData = await GetUserData(user).ConfigureAwait(false);

            if (!maybeUserData.HasValue) // nothing configured for this user
            {
                return AuthenticationResult.Failure(user);
            }

            var userData = maybeUserData.Value;

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

            if (string.IsNullOrEmpty(authenticationData.Password))
            {
                return AuthenticationResult.Failure(user);
            }

            var mfaEnabled = userData.TOTPSecret is not null;
            var attemptedPassword = authenticationData.Password;
            var attemptedTOTP = authenticationData.TOTP;

            PasswordHash readyHash = PasswordHash.Parse(userData.PasswordHash);
            if (!cryptographyProvider.Verify(readyHash, attemptedPassword))
            {
                if (mfaEnabled // mfa enabled for this account
                    && attemptedTOTP is null // no explicit TOTP code provided
                    && attemptedPassword.Length > TOTPCodeLength // we can safely substring the password
                    && cryptographyProvider.Verify(readyHash, attemptedPassword.AsSpan()[..^TOTPCodeLength])) // substringed password is valid
                {
                    attemptedTOTP = attemptedPassword[^TOTPCodeLength..];
                    attemptedPassword = attemptedPassword[..^TOTPCodeLength];
                }
                else
                {
                    return AuthenticationResult.Failure(user);
                }
            }

            // Migrate old hashes to the new default
            if (!string.Equals(readyHash.Id, cryptographyProvider.DefaultHashMethod, StringComparison.Ordinal)
                || int.Parse(readyHash.Parameters["iterations"], CultureInfo.InvariantCulture) != Constants.DefaultIterations)
            {
                logger.LogInformation("Migrating password hash of {User} to the latest default", user.Username);
                await ChangePassword(user, attemptedPassword).ConfigureAwait(false);
            }

            if (!mfaEnabled) // no mfa, done here
            {
                return AuthenticationResult.Success(user);
            }

            var settingUpTOTP = false;

            if (!userData.MfaSetup) // MFA required but was not yet set up
            {
                if (attemptedTOTP is null) // client has not sent a TOTP with this request, inform them that setup is required
                {
                    return AuthenticationResult.Failure(user, 1301, new OtpUri(OtpType.Totp, userData.TOTPSecret, user.Username, issuer: "Jellyfin", digits: TOTPCodeLength).ToString());
                }

                settingUpTOTP = true; // since client has included a TOTP code on a user that has not set up their MFA yet, they must be trying to set up just now
            }

            if (attemptedTOTP is null)
            {
                return AuthenticationResult.Failure(user, 1300); // incorrect or missing TOTP
            }

            var totpVerifier = new Totp(userData.TOTPSecret, totpSize: TOTPCodeLength);
            if (!totpVerifier.VerifyTotp(attemptedTOTP, out _, VerificationWindow.RfcSpecifiedNetworkDelay))
            {
                return AuthenticationResult.Failure(user, 1300); // incorrect or missing TOTP
            }

            if (settingUpTOTP) // just successfully completed setup, persist this
            {
                await SaveUserData(user, new DefaultAuthProviderUserData()
                {
                    MfaSetup = true,
                    TOTPSecret = userData.TOTPSecret,
                    PasswordHash = userData.PasswordHash,
                }).ConfigureAwait(false);
            }

            return AuthenticationResult.Success(user);
        }

        /// <summary>
        /// Enables or disables MFA for a given user.
        /// </summary>
        /// <param name="user">The user for whom to enable or disable MFA.</param>
        /// <param name="enable">Set to true to enable, set to false to disable.</param>
        /// <returns>A completed task.</returns>
        public async Task SetMFA(User user, bool enable)
        {
            var maybeData = await GetUserData(user).ConfigureAwait(false);

            if (!maybeData.HasValue)
            {
                throw new InvalidOperationException("Cannot set MFA on a user without any authentication data");
            }

            var userData = maybeData.Value;

            if (enable == (userData.TOTPSecret is not null)) // want to enable and already enabled, or want to disable and already disabled; do nothing
            {
                return;
            }

            if (!enable)
            {
                await SaveUserData(user, new DefaultAuthProviderUserData()
                {
                    MfaSetup = false,
                    TOTPSecret = null,
                    PasswordHash = userData.PasswordHash,
                }).ConfigureAwait(false);
                return;
            }

            await SaveUserData(user, new DefaultAuthProviderUserData()
            {
                MfaSetup = false,
                TOTPSecret = RandomNumberGenerator.GetBytes(32),
                PasswordHash = userData.PasswordHash,
            }).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override Task<NoData> InitialGlobalData()
        {
            return Task.FromResult(default(NoData));
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
                await SaveUserData(user, data => new DefaultAuthProviderUserData()
                {
                    PasswordHash = null,
                    MfaSetup = data.HasValue && data.Value.MfaSetup,
                    TOTPSecret = data.HasValue ? data.Value.TOTPSecret : null
                }).ConfigureAwait(false);
                return;
            }

            PasswordHash newPasswordHash = cryptographyProvider.CreatePasswordHash(newPassword);
            await SaveUserData(user, data => new DefaultAuthProviderUserData()
            {
                PasswordHash = newPasswordHash.ToString(),
                MfaSetup = data.HasValue && data.Value.MfaSetup,
                TOTPSecret = data.HasValue ? data.Value.TOTPSecret : null
            }).ConfigureAwait(false);

            await eventManager.PublishAsync(new UserPasswordChangedEventArgs(user)).ConfigureAwait(false);
        }
    }

#pragma warning disable SA1201, CA1819 // Elements should appear in the correct order, Properties should not return arrays
    /// <summary>
    /// User data for the default authentication provider.
    /// </summary>
    /// <param name="PasswordHash">Hashed user password.</param>
    /// <param name="MfaSetup">Whether or not MFA was successfully set up by this user.</param>
    /// <param name="TOTPSecret">The TOTP secret for this user.</param>
    public record struct DefaultAuthProviderUserData(string? PasswordHash, bool MfaSetup, byte[]? TOTPSecret);
#pragma warning restore SA1201, CA1819 // Elements should appear in the correct order, Properties should not return arrays
}
