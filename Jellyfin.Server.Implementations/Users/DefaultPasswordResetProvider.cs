using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Users;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// The default password reset provider.
    /// </summary>
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        private const string BaseResetFileName = "passwordreset";

        private readonly IApplicationHost _appHost;

        private readonly string _passwordResetFileBase;
        private readonly string _passwordResetFileBaseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPasswordResetProvider"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="appHost">The application host.</param>
        public DefaultPasswordResetProvider(IServerConfigurationManager configurationManager, IApplicationHost appHost)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, BaseResetFileName);
            _appHost = appHost;
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

                    await userManager.ChangePassword(resetUser, pin).ConfigureAwait(false);
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
        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(User user, bool isInNetwork)
        {
            byte[] bytes = new byte[4];
            RandomNumberGenerator.Fill(bytes);
            string pin = BitConverter.ToString(bytes);

            DateTime expireTime = DateTime.UtcNow.AddMinutes(30);
            string filePath = _passwordResetFileBase + user.Id + ".json";
            SerializablePasswordReset spr = new SerializablePasswordReset
            {
                ExpirationDate = expireTime,
                Pin = pin,
                PinFile = filePath,
                UserName = user.Username
            };

            FileStream fileStream = AsyncFile.OpenWrite(filePath);
            await using (fileStream.ConfigureAwait(false))
            {
                await JsonSerializer.SerializeAsync(fileStream, spr).ConfigureAwait(false);
            }

            return new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.PinCode,
                PinExpirationDate = expireTime,
                PinFile = filePath
            };
        }

#nullable disable
        private class SerializablePasswordReset : PasswordPinCreationResult
        {
            public string Pin { get; set; }

            public string UserName { get; set; }
        }
    }
}
