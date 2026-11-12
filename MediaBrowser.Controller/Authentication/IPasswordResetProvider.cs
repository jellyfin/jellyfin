#pragma warning disable CS1591

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
}
