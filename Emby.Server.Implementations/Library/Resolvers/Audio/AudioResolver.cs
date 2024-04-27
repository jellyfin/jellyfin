#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver.
    /// </summary>
    public class AudioResolver : ItemResolver<MediaBrowser.Controller.Entities.Audio.Audio>, IMultiItemResolver
    {
        private readonly NamingOptions _namingOptions;

        public AudioResolver(NamingOptions namingOptions)
        {
            _namingOptions = namingOptions;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Fifth;

        public MultiItemResolverResult ResolveMultiple(
            Folder parent,
            List<FileSystemMetadata> files,
            CollectionType? collectionType,
            IDirectoryService directoryService)
        {
            return null;
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override MediaBrowser.Controller.Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            var collectionType = args.GetCollectionType();

            if (args.IsDirectory)
            {
                return null;
            }

            if (AudioFileParser.IsAudioFile(args.Path, _namingOptions))
            {
                var extension = Path.GetExtension(args.Path.AsSpan());

                if (extension.Equals(".cue", StringComparison.OrdinalIgnoreCase))
                {
                    // if audio file exists of same name, return null
                    return null;
                }

                var isMixedCollectionType = collectionType is null;

                // For conflicting extensions, give priority to videos
                if (isMixedCollectionType && VideoResolver.IsVideoFile(args.Path, _namingOptions))
                {
                    return null;
                }

                MediaBrowser.Controller.Entities.Audio.Audio item = null;

                var isMusicCollectionType = collectionType == CollectionType.music;

                // Use regular audio type for mixed libraries, owned items and music
                if (isMixedCollectionType ||
                    args.Parent is null ||
                    isMusicCollectionType)
                {
                    item = new MediaBrowser.Controller.Entities.Audio.Audio();
                }

                if (item is not null)
                {
                    item.IsShortcut = extension.Equals(".strm", StringComparison.OrdinalIgnoreCase);

                    item.IsInMixedFolder = true;
                }

                return item;
            }

            return null;
        }
    }
}
