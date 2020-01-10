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

namespace Emby.Server.Implementations.Library
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
            List<string> usersreset = new List<string>();
            foreach (var resetfile in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{BaseResetFileName}*"))
            {
                using (var str = File.OpenRead(resetfile))
                {
                    spr = await _jsonSerializer.DeserializeFromStreamAsync<SerializablePasswordReset>(str).ConfigureAwait(false);
                }

                if (spr.ExpirationDate < DateTime.Now)
                {
                    File.Delete(resetfile);
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
                    usersreset.Add(resetUser.Name);
                    File.Delete(resetfile);
                }
            }

            if (usersreset.Count < 1)
            {
                throw new ResourceNotFoundException($"No Users found with a password reset request matching pin {pin}");
            }
            else
            {
                return new PinRedeemResult
                {
                    Success = true,
                    UsersReset = usersreset.ToArray()
                };
            }
        }

        /// <inheritdoc />
        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(MediaBrowser.Controller.Entities.User user, bool isInNetwork)
        {
            string pin = string.Empty;
            using (var cryptoRandom = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[4];
                cryptoRandom.GetBytes(bytes);
                pin = BitConverter.ToString(bytes);
            }

            DateTime expireTime = DateTime.Now.AddMinutes(30);
            string filePath = _passwordResetFileBase + user.InternalId + ".json";
            SerializablePasswordReset spr = new SerializablePasswordReset
            {
                ExpirationDate = expireTime,
                Pin = pin,
                PinFile = filePath,
                UserName = user.Name
            };

            using (FileStream fileStream = File.OpenWrite(filePath))
            {
                _jsonSerializer.SerializeToStream(spr, fileStream);
                await fileStream.FlushAsync().ConfigureAwait(false);
            }

            return new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.PinCode,
                PinExpirationDate = expireTime,
                PinFile = filePath
            };
        }

        private class SerializablePasswordReset : PasswordPinCreationResult
        {
            public string Pin { get; set; }

            public string UserName { get; set; }
        }
    }
}
