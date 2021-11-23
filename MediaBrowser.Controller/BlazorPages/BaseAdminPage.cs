using System.Threading.Tasks;
using Jellyfin.Data.Enums;

namespace MediaBrowser.Controller.BlazorPages;

/// <summary>
/// Page that requires admin authentication.
/// </summary>
public abstract class BaseAdminPage : BasePage
{
    /// <inheritdoc />
    protected override async Task OnInitializedAsync()
    {
        if (string.IsNullOrEmpty(Token))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        var authorizationInfo = await AuthorizationContext.GetAuthorizationInfo(Token)
            .ConfigureAwait(false);
        if (!authorizationInfo.IsAuthenticated
            || !authorizationInfo.User.HasPermission(PermissionKind.IsAdministrator))
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        await base.OnInitializedAsync().ConfigureAwait(false);
    }
}
