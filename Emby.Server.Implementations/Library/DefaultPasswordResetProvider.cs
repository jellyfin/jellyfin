using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Cryptography;
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

        private readonly IJsonSerializer _jsonSerializer;
        private readonly IUserManager _userManager;
        private readonly ICryptoProvider _crypto;

        public DefaultPasswordResetProvider(IServerConfigurationManager configurationManager, IJsonSerializer jsonSerializer, IUserManager userManager, ICryptoProvider cryptoProvider)
        {
            _passwordResetFileBaseDir = configurationManager.ApplicationPaths.ProgramDataPath;
            _passwordResetFileBase = Path.Combine(_passwordResetFileBaseDir, _passwordResetFileBaseName);
            _jsonSerializer = jsonSerializer;
            _userManager = userManager;
            _crypto = cryptoProvider;
        }

        public async Task<PinRedeemResult> RedeemPasswordResetPin(string pin)
        {
            SerializablePasswordReset spr;
            HashSet<string> usersreset = new HashSet<string>();
            foreach (var resetfile in Directory.EnumerateFiles(_passwordResetFileBaseDir, $"{_passwordResetFileBaseName}*"))
            {
                using (var str = File.OpenRead(resetfile))
                {
                    spr = await _jsonSerializer.DeserializeFromStreamAsync<SerializablePasswordReset>(str).ConfigureAwait(false);
                }

                if (spr.ExpirationDate < DateTime.Now)
                {
                    File.Delete(resetfile);
                }
                else if (spr.Pin.Replace("-", "").Equals(pin.Replace("-", ""), StringComparison.InvariantCultureIgnoreCase))
                {
                    var resetUser = _userManager.GetUserByName(spr.UserName);
                    if (resetUser == null)
                    {
                        throw new Exception($"User with a username of {spr.UserName} not found");
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

        public async Task<ForgotPasswordResult> StartForgotPasswordProcess(MediaBrowser.Controller.Entities.User user, bool isInNetwork)
        {
            string pin = string.Empty;
            using (var cryptoRandom = System.Security.Cryptography.RandomNumberGenerator.Create())
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

            try
            {
                using (FileStream fileStream = File.OpenWrite(filePath))
                {
                    _jsonSerializer.SerializeToStream(spr, fileStream);
                    await fileStream.FlushAsync().ConfigureAwait(false);
                }
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
