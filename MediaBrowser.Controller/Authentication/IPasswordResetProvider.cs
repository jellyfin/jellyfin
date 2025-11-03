#pragma warning disable CS1591

using System;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
{
    public interface IPasswordResetProvider
    {
        string Name { get; }

        bool IsEnabled { get; }

        Task<ForgotPasswordResult> StartForgotPasswordProcess(User? user, string enteredUsername, bool isInNetwork);

        Task<PinRedeemResult> RedeemPasswordResetPin(string pin);
    }

#nullable disable
    public class PasswordPinCreationResult
    {
        public string PinFile { get; set; }

        public DateTime ExpirationDate { get; set; }
    }
}
