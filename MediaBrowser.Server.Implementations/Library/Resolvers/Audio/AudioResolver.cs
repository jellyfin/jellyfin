using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;

namespace MediaBrowser.Server.Implementations.Library.Resolvers.Audio
{
    /// <summary>
    /// Class AudioResolver
    /// </summary>
    public class AudioResolver : ItemResolver<Controller.Entities.Audio.Audio>
    {
        private readonly ILibraryManager _libraryManager;

        public AudioResolver(ILibraryManager libraryManager)
        {
            _libraryManager = libraryManager;
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>Entities.Audio.Audio.</returns>
        protected override Controller.Entities.Audio.Audio Resolve(ItemResolveArgs args)
        {
            // Return audio if the path is a file and has a matching extension

            if (!args.IsDirectory)
            {
                var libraryOptions = args.GetLibraryOptions();

                if (_libraryManager.IsAudioFile(args.Path, libraryOptions))
                {
                    var collectionType = args.GetCollectionType();

                    var isMixed = string.IsNullOrWhiteSpace(collectionType);

                    // For conflicting extensions, give priority to videos
                    if (isMixed && _libraryManager.IsVideoFile(args.Path, libraryOptions))
                    {
                        return null;
                    }

                    var isStandalone = args.Parent == null;

                    if (isStandalone ||
                        string.Equals(collectionType, CollectionType.Music, StringComparison.OrdinalIgnoreCase) ||
                        isMixed)
                    {
                        return new Controller.Entities.Audio.Audio();
                    }
                }
            }

            return null;
        }
    }
}
