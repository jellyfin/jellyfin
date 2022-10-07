#nullable disable

using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into an instance of the <see cref="Video"/> class.
    /// </summary>
    /// <typeparam name="T">The type of item to resolve.</typeparam>
    public class GenericVideoResolver<T> : BaseVideoResolver<T>
        where T : Video, new()
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="GenericVideoResolver{T}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        public GenericVideoResolver(ILogger logger, NamingOptions namingOptions)
            : base(logger, namingOptions)
        {
        }
    }
}
