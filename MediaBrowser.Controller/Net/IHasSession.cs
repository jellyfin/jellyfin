
namespace MediaBrowser.Controller.Net
{
    public interface IHasSession
    {
        /// <summary>
        /// Gets or sets the session context.
        /// </summary>
        /// <value>The session context.</value>
        ISessionContext SessionContext { get; set; }
    }
}
