using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Users;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// The default password reset provider.
    /// </summary>
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        private const string BaseResetFileName = "passwordreset";

        private readonly IApplicationHost _appHost;
        private readonly ILogger<DefaultPasswordResetProvider> _logger;

        private readonly string _passwordResetFileBase;
        private readonly string _passwordResetFileBaseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPasswordResetProvider"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The application host.</param>
        /// <param name="logger">The logger.</param>
        public DefaultPasswordResetProvider(IServerConfigurationManager configurationManager, IApplicationHost appHost, ILogger<DefaultPasswordResetProvider> logger)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, BaseResetFileName);
            _appHost = appHost;
            _logger = logger;
            // TODO: Remove the circular dependency on UserManager
        }

        /// <inheritdoc />
        public string Name => "Default Password Reset Provider";

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            var userManager = _appHost.Resolve<IUserManager>();
            var usersReset = new List<string>();
            foreach (var resetFile in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{BaseResetFileName}*"))
            {
                SerializablePasswordReset spr;
                var str = AsyncFile.OpenRead(resetFile);
                await using (str.ConfigureAwait(false))
                {
                    spr = await JsonSerializer.DeserializeAsync<SerializablePasswordReset>(str).ConfigureAwait(false)
                        ?? throw new ResourceNotFoundException($"Provided path ({resetFile}) is not valid.");
                }

                if (spr.ExpirationDate < DateTime.UtcNow)
                {
                    File.Delete(resetFile);
                }
                else if (string.Equals(
                    spr.Pin.Replace("-", string.Empty, StringComparison.Ordinal),
                    pin.Replace("-", string.Empty, StringComparison.Ordinal),
                    StringComparison.Ordinal))
                {
                    var resetUser = userManager.GetUserByName(spr.UserName)
                        ?? throw new ResourceNotFoundException($"User with a username of {spr.UserName} not found");

                    await userManager.ChangePassword(resetUser.Id, pin).ConfigureAwait(false);
                    usersReset.Add(resetUser.Username);
                    File.Delete(resetFile);
                }
            }

            if (usersReset.Count < 1)
            {
                throw new ResourceNotFoundException($"No Users found with a password reset request matching pin {pin}");
            }

            return new PinRedeemResult
            {
                Success = true,
                UsersReset = usersReset.ToArray()
            };
        }

        /// <inheritdoc />
        public Task<ForgotPasswordResult> StartForgotPasswordProcess(User? user, string enteredUsername, bool isInNetwork)
        {
            DateTime expireTime = DateTime.UtcNow.AddMinutes(30);
            var usernameHash = enteredUsername.ToUpperInvariant().GetMD5().ToString("N", CultureInfo.InvariantCulture);
            var pinFile = _passwordResetFileBase + usernameHash + ".json";

            if (user is not null && isInNetwork)
            {
                // Write the PIN file off the request path: the result below does not depend on it,
                // and waiting for it would make the response time reveal whether the user exists.
                var username = user.Username;
                _ = Task.Run(() => WritePinFileAsync(pinFile, username, expireTime));
            }

            return Task.FromResult(new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.PinCode,
                PinExpirationDate = expireTime,
                PinFile = pinFile
            });
        }

        private async Task WritePinFileAsync(string pinFile, string username, DateTime expireTime)
        {
            // Write to a temporary file and then move it into place, so that PIN redemption never reads a half-written file.
            // The temporary name must not start with the reset file name, because redemption enumerates files by that prefix.
            var tempFile = Path.Combine(_passwordResetFileBaseDir, Path.GetRandomFileName() + ".tmp");
            try
            {
                byte[] bytes = new byte[4];
                RandomNumberGenerator.Fill(bytes);
                string pin = BitConverter.ToString(bytes);

                SerializablePasswordReset spr = new SerializablePasswordReset
                {
                    ExpirationDate = expireTime,
                    Pin = pin,
                    PinFile = pinFile,
                    UserName = username
                };

                FileStream fileStream = AsyncFile.Create(tempFile);
                await using (fileStream.ConfigureAwait(false))
                {
                    await JsonSerializer.SerializeAsync(fileStream, spr).ConfigureAwait(false);
                }

                File.Move(tempFile, pinFile, overwrite: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write password reset PIN file {PinFile}", pinFile);
                TryDeleteTempFile(tempFile);
            }
        }

        private void TryDeleteTempFile(string tempFile)
        {
            try
            {
                File.Delete(tempFile);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogDebug(ex, "Failed to delete temporary password reset file {TempFile}", tempFile);
            }
        }

#nullable disable
        private class SerializablePasswordReset : PasswordPinCreationResult
        {
            public string Pin { get; set; }

            public string UserName { get; set; }
        }
    }
}
