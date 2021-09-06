#nullable disable

#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Data.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
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
