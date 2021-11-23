using MediaBrowser.Controller.Net;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.WebUtilities;

namespace MediaBrowser.Controller.BlazorPages;

/// <summary>
/// Page that requires admin authentication.
/// </summary>
public abstract class BasePage : ComponentBase
{
    /// <summary>
    /// Gets the the auth context.
    /// </summary>
    [Inject]
    public IAuthorizationContext AuthorizationContext { get; init; } = null!;

    /// <summary>
    /// Gets the navigation manager.
    /// </summary>
    [Inject]
    public NavigationManager NavigationManager { get; init; } = null!;

    /// <summary>
    /// Gets token parameter.
    /// </summary>
    [Parameter]
    [SupplyParameterFromQuery]
    public string? Token { get; init; }

    /// <summary>
    /// Navigate to the provided url, adding the token query parameter.
    /// </summary>
    /// <param name="url">The url to navigate to.</param>
    protected void Navigate(string url)
    {
        if (!string.IsNullOrEmpty(Token))
        {
            url = QueryHelpers.AddQueryString(url, "token", Token);
        }

        NavigationManager.NavigateTo(url);
    }
}
