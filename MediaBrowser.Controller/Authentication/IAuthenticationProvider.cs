#pragma warning disable CS1591

using System.Threading.Tasks;
using Jellyfin.Data.Entities;
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
    }

    public interface IRequiresResolvedUser
    {
        Task<ProviderAuthenticationResult> Authenticate(string username, string password, User? resolvedUser);
    }

    public interface IHasNewUserPolicy
    {
        UserPolicy GetNewUserPolicy();
    }

    public class ProviderAuthenticationResult
    {
        public required string Username { get; set; }

        public string? DisplayName { get; set; }
    }
}
