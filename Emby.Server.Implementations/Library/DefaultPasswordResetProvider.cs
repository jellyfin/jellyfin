using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;

namespace Emby.Server.Implementations.Library
{
    public class DefaultPasswordResetProvider : IPasswordResetProvider
    {
        public string Name => "Default Password Reset Provider";

        public bool IsEnabled => true;

        private readonly string _passwordResetFileBase;
        private readonly string _passwordResetFileBaseDir;
        private readonly string _passwordResetFileBaseName = "passwordreset";

        private IJsonSerializer _jsonSerializer;
        private IUserManager _userManager;

        public DefaultPasswordResetProvider(IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IUserManager userManager)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, _passwordResetFileBaseName);
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
        }

        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            HashSet<string> usersreset = new HashSet<string>();
            foreach (var resetfile in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{_passwordResetFileBaseName}*"))
            {
                var spr = (SerializablePasswordReset) _jsonSerializer.DeserializeFromFile(typeof(SerializablePasswordReset), resetfile);
                if (spr.ExpirationDate < DateTime.Now)
                {
                    File.Delete(resetfile);
                }
                else
                {
                    if (spr.Pin == pin)
                    {
                        var resetUser = _userManager.GetUserByName(spr.UserName);
                        if (!string.IsNullOrEmpty(resetUser.Password))
                        {
                            await _userManager.ChangePassword(resetUser, pin).ConfigureAwait(false);
                            usersreset.Add(resetUser.Name);
                        }
                    }
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

            throw new System.NotImplementedException();
        }

        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(MediaBrowser.Controller.Entities.User user, bool isInNetwork)
        {
            string pin = new Random().Next(99999999).ToString("00000000",CultureInfo.InvariantCulture);
            DateTime expireTime = DateTime.Now.AddMinutes(30);
            string filePath = _passwordResetFileBase + user.Name.ToLowerInvariant() + ".json";
            SerializablePasswordReset spr = new SerializablePasswordReset
            {
                ExpirationDate = expireTime,
                Pin = pin,
                PinFile = filePath,
                UserName = user.Name
            };

            try
            {
                await Task.Run(() => File.WriteAllText(filePath, _jsonSerializer.SerializeToString(spr))).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                throw new Exception($"Error serializing or writing password reset for {user.Name} to location: {filePath}", e);
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
