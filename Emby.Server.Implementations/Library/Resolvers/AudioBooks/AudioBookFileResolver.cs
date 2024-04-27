#nullable disable

#pragma warning disable SA1642

using System;
using System.IO;
using System.Linq;
using Emby.Naming.Common;
using Jellyfin.Extensions;
using MediaBrowser.Controller.Entities.AudioBooks;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using Microsoft.Extensions.Logging;

namespace Emby.Server.Implementations.Library.Resolvers.AudioBooks
{
    /// <summary>
    /// Class AudioBookFileResolver.
    /// NOTE: Should these files be moved to MediaBrowser.Controller.Resolvers.(AudioBooks?) directory?.
    /// </summary>
    public class AudioBookFileResolver : BaseAudioBookResolver<AudioBookFile>
    {
        private readonly ILogger<AudioBookFileResolver> _logger;
        private readonly NamingOptions _namingOptions;

        /// <summary>
        /// Initializes a new instance of the AudioBookFileResolver class.
        /// </summary>
        /// <param name="logger">The logger.</param>
        /// <param name="namingOptions">Options for naming.</param>
        /// <param name="directoryService">Some other thing.</param>
        public AudioBookFileResolver(ILogger<AudioBookFileResolver> logger, NamingOptions namingOptions, IDirectoryService directoryService)
            : base(logger, namingOptions, directoryService)
        {
            _logger = logger;
            _namingOptions = namingOptions;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Fourth;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Episode.</returns>
        protected override AudioBookFile Resolve(ItemResolveArgs args)
        {
            if (!IsValid(args))
            {
                return null;
            }

            return ResolveAudioBookFile<AudioBookFile>(args, true);
        }

        private bool IsValid(ItemResolveArgs args)
        {
            if (args is null)
            {
                return false;
            }

            var parent = args.Parent;

            if (parent is null || args.IsDirectory)
            {
                return false;
            }

            if (parent.GetType() != typeof(AudioBook))
            {
                return false;
            }

            if (args.CollectionType != Jellyfin.Data.Enums.CollectionType.books)
            {
                return false;
            }

            var path = args.Path;
            if (path.Length == 0 || Path.GetFileNameWithoutExtension(path).Length == 0)
            {
                return false;
            }

            if (!_namingOptions.AudioFileExtensions.Contains(Path.GetExtension(args.Path), StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return true;
        }
    }
}
