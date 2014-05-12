using System;
using MediaBrowser.Model.Logging;
using ServiceStack.Logging;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class ServerLogFactory
    /// </summary>
    public class ServerLogFactory : ILogFactory
    {
        /// <summary>
        /// The _log manager
        /// </summary>
        private readonly ILogManager _logManager;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerLogFactory"/> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        public ServerLogFactory(ILogManager logManager)
        {
            _logManager = logManager;
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="typeName">Name of the type.</param>
        /// <returns>ILog.</returns>
        public ILog GetLogger(string typeName)
        {
            return new ServerLogger(_logManager.GetLogger(typeName));
        }

        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns>ILog.</returns>
        public ILog GetLogger(Type type)
        {
            return GetLogger(type.Name);
        }
    }
}