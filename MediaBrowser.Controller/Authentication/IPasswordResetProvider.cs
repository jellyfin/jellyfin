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

        Task<ForgotPasswordResult> StartForgotPasswordProcess(User user);

        Task<CodeRedeemResult> RedeemPasswordResetPin(string code, string password);
    }

    public class PasswordResetResult
    {
        public string File { get; set; }

        public DateTime ExpirationDate { get; set; }
    }
}
