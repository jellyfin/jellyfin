#nullable disable

using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
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
        private readonly bool _parseName;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericVideoResolver{T}"/> class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">The naming options.</param>
        /// <param name="directoryService">The directory service.</param>
        /// <param name="parseName">Whether to parse the file name for metadata such as the year.</param>
        public GenericVideoResolver(ILogger logger, NamingOptions namingOptions, IDirectoryService directoryService, bool parseName = false)
            : base(logger, namingOptions, directoryService)
        {
            _parseName = parseName;
        }

        /// <inheritdoc />
        protected override T Resolve(ItemResolveArgs args)
        {
            return ResolveVideo<T>(args, _parseName);
        }
    }
}
