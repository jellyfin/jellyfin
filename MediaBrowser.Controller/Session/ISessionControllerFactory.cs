
namespace MediaBrowser.Controller.Session
{
    /// <summary>
    /// Interface ISesssionControllerFactory
    /// </summary>
    public interface ISessionControllerFactory
    {
        /// <summary>
        /// Gets the session controller.
        /// </summary>
        /// <param name="session">The session.</param>
        /// <returns>ISessionController.</returns>
        ISessionController GetSessionController(SessionInfo session);
    }
}
