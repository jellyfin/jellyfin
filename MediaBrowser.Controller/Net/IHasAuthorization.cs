
namespace MediaBrowser.Controller.Net
{
    public interface IHasAuthorization
    {
        /// <summary>
        /// Gets or sets the authorization context.
        /// </summary>
        /// <value>The authorization context.</value>
        IAuthorizationContext AuthorizationContext { get; set; }
    }
}
