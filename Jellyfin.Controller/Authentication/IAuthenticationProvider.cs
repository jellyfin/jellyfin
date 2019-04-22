using System.Threading.Tasks;
using Jellyfin.Controller.Entities;
using Jellyfin.Model.Users;

namespace Jellyfin.Controller.Authentication
{
    public interface IAuthenticationProvider
    {
        string Name { get; }
        bool IsEnabled { get; }
        Task<ProviderAuthenticationResult> Authenticate(string username, string password);
        Task<bool> HasPassword(User user);
        Task ChangePassword(User user, string newPassword);
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
