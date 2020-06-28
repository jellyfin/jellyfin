#nullable enable

using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Activity;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;

namespace Jellyfin.Server.Implementations.Users
{
    /// <summary>
    /// The default password reset provider.
    /// </summary>
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        private const string BaseResetFileName = "passwordreset";

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;
        private readonly IActivityManager _activityManager;
        private readonly ILocalizationManager _localization;

        private readonly string _passwordResetFileBase;
        private readonly string _passwordResetFileBaseDir;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultPasswordResetProvider"/> class.
        /// </summary>
        /// <param name="configurationManager">The configuration manager.</param>
        /// <param name="jsonSerializer">The JSON serializer.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="activityManager">The activity manager.</param>
        /// <param name="localization">The localization manager.</param>
        public DefaultPasswordResetProvider(
            IServerConfigurationManager configurationManager,
            IJsonSerializer jsonSerializer,
            IUserManager userManager,
            IActivityManager activityManager,
            ILocalizationManager localization)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, BaseResetFileName);
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _activityManager = activityManager;
            _localization = localization;
        }

        /// <inheritdoc />
        public string Name => "Default Password Reset Provider";

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public async Task<CodeRedeemResult> RedeemPasswordResetPin(string code, string password)
        {
            SerializablePasswordReset passwordReset;
            foreach (var file in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{BaseResetFileName}*"))
            {
                using (var stream = File.OpenRead(file))
                {
                    passwordReset = await _jsonSerializer.DeserializeFromStreamAsync<SerializablePasswordReset>(stream).ConfigureAwait(false);
                }

                if (passwordReset.ExpirationDate < DateTime.Now)
                {
                    File.Delete(file);
                }
                else if (string.Equals(passwordReset.Code, code, StringComparison.InvariantCultureIgnoreCase))
                {
                    var resetUser = _userManager.GetUserByName(passwordReset.Username);
                    if (resetUser == null)
                    {
                        File.Delete(file);
                        continue;
                    }

                    await _userManager.ChangePassword(resetUser, password).ConfigureAwait(false);
                    File.Delete(file);

                    return new CodeRedeemResult
                    {
                        Success = true
                    };
                }
            }

            return new CodeRedeemResult
            {
                Success = false
            };
        }

        /// <inheritdoc />
        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(User user)
        {
            string code = string.Empty;
            using (var cryptoRandom = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[10];
                cryptoRandom.GetBytes(bytes);
                code = BitConverter.ToString(bytes);
            }

            DateTime expireTime = DateTime.Now.AddMinutes(30);
            string filePath = _passwordResetFileBase + user.InternalId + ".json";

            SerializablePasswordReset passwordReset = new SerializablePasswordReset
            {
                ExpirationDate = expireTime,
                Code = code,
                File = filePath,
                Username = user.Username
            };

            await using (FileStream fileStream = File.OpenWrite(filePath))
            {
                _jsonSerializer.SerializeToStream(passwordReset, fileStream);
                await fileStream.FlushAsync().ConfigureAwait(false);
            }

            var response = new ForgotPasswordResult
            {
                Action = ForgotPasswordAction.PinCode,
                ExpirationDate = expireTime
            };

            _activityManager.Create(new ActivityLog(
                string.Format(
                    CultureInfo.InvariantCulture,
                    _localization.GetLocalizedString("UserPasswordReset"),
                    user.Username,
                    code),
                "UserPolicyUpdated",
                user.Id));

            return response;
        }

        private class SerializablePasswordReset : PasswordResetResult
        {
            public string? Code { get; set; }

            public string? Username { get; set; }
        }
    }
}
