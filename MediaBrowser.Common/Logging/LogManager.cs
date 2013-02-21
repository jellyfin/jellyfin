using MediaBrowser.Model.Logging;

namespace MediaBrowser.Common.Logging
{
    /// <summary>
    /// Class LogManager
    /// </summary>
    public static class LogManager
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>ILogger.</returns>
        public static ILogger GetLogger(string name)
        {
            return new NLogger(name);
        }
    }
}
