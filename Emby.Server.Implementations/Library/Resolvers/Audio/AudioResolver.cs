#nullable disable

#pragma warning disable CS1591

using System;
using System.IO;
using Emby.Naming.Audio;
using Emby.Naming.Common;
using Emby.Naming.Video;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

namespace Emby.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver.
    /// </summary>
    public class AudioResolver : ItemResolver<MediaBrowser.Controller.Entities.Audio.Audio>
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

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override MediaBrowser.Controller.Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            var collectionType = args.GetCollectionType();

            var isBooksCollectionType = collectionType == CollectionType.books;

            // Directories in book collections are handled by AudioBookResolver
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

                // All audio files (including in book collections) resolve as Audio.
                // In book collections, the parent AudioBook folder provides the grouping.
                if (isMixedCollectionType ||
                    args.Parent is null ||
                    collectionType == CollectionType.music ||
                    isBooksCollectionType)
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
