using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;

namespace Jellyfin.Server.Implementations.User
{
    /// <summary>
    /// The default password reset provider.
    /// </summary>
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        private const string BaseResetFileName = "passwordreset";

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;

        private readonly string _passwordResetFileBase;
        private readonly string _passwordResetFileBaseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPasswordResetProvider"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The JSON serializer.</param>
        /// <param name="userManager">The user manager.</param>
        public DefaultPasswordResetProvider(
            IServerConfigurationManager configurationManager,
            IJsonSerializer jsonSerializer,
            IUserManager userManager)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, BaseResetFileName);
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
        }

        /// <inheritdoc />
        public string Name => "Default Password Reset Provider";

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            SerializablePasswordReset spr;
            List<string> usersReset = new List<string>();
            foreach (var resetFile in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{BaseResetFileName}*"))
            {
                await using (var str = File.OpenRead(resetFile))
                {
                    spr = await _jsonSerializer.DeserializeFromStreamAsync<SerializablePasswordReset>(str).ConfigureAwait(false);
                }

                if (spr.ExpirationDate < DateTime.Now)
                {
                    File.Delete(resetFile);
                }
                else if (string.Equals(
                    spr.Pin.Replace("-", string.Empty, StringComparison.Ordinal),
                    pin.Replace("-", string.Empty, StringComparison.Ordinal),
                    StringComparison.InvariantCultureIgnoreCase))
                {
                    var resetUser = _userManager.GetUserByName(spr.UserName);
                    if (resetUser == null)
                    {
                        throw new ResourceNotFoundException($"User with a username of {spr.UserName} not found");
                    }

                    await _userManager.ChangePassword(resetUser, pin).ConfigureAwait(false);
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
        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(Jellyfin.Data.Entities.User user, bool isInNetwork)
        {
            string pin;
            using (var cryptoRandom = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                cryptoRandom.GetBytes(bytes);
                pin = BitConverter.ToString(bytes);
            }

            DateTime expireTime = DateTime.Now.AddMinutes(30);

            user.EasyPassword = pin;
            await _userManager.UpdateUserAsync(user).ConfigureAwait(false);

            return new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.PinCode,
                PinExpirationDate = expireTime,
            };
        }

        private class SerializablePasswordReset : PasswordPinCreationResult
        {
            public string Pin { get; set; }

            public string UserName { get; set; }
        }
    }
}
