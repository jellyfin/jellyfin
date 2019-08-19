using System.Threading.Tasks;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Users;

namespace MediaBrowser.Controller.Authentication
{
    public interface IAuthenticationProvider
    {
        string Name { get; }
        bool IsEnabled { get; }
        Task<ProviderAuthenticationResult> Authenticate(string username, string password);
        bool HasPassword(User user);
        Task ChangePassword(User user, string newPassword);
        void ChangeEasyPassword(User user, string newPassword, string newPasswordHash);
        string GetEasyPasswordHash(User user);
    }

    public interface IRequiresResolvedUser
    {
        Task<ProviderAuthenticationResult> Authenticate(string username, string password, User resolvedUser);
    }

    public interface IHasNewUserPolicy
    {
        UserPolicy GetNewUserPolicy();
    }

    public class ProviderAuthenticationResult
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
    }
}
