using System.Threading.Tasks;

namespace MediaBrowser.Controller.BlazorPages;

/// <summary>
/// Page that requires authentication.
/// </summary>
public abstract class BaseAuthenticatedPage : BasePage
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
        if (!authorizationInfo.IsAuthenticated)
        {
            NavigationManager.NavigateTo("/");
            return;
        }

        await base.OnInitializedAsync().ConfigureAwait(false);
    }
}
