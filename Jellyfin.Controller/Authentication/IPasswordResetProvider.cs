using System;
using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Users;

namespace Jellyfin.Controller.Authentication
{
    public interface IPasswordResetProvider
    {
        string Name { get; }
        bool IsEnabled { get; }
        Task<ForgotPasswordResult> StartForgotPasswordProcess(User user, bool isInNetwork);
        Task<PinRedeemResult> RedeemPasswordResetPin(string pin);
    }
    public class PasswordPinCreationResult
    {
        public string PinFile { get; set; }
        public DateTime ExpirationDate { get; set; }
    }
}
