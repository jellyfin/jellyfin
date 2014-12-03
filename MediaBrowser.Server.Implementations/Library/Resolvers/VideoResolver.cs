using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;
using System;
using System.Linq;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video
    /// </summary>
    public class VideoResolver : BaseVideoResolver<Video>
    {
        public VideoResolver(ILibraryManager libraryManager)
            : base(libraryManager)
        {
        }

        protected override Video Resolve(ItemResolveArgs args)
        {
            if (args.Parent != null)
            {
                // The movie resolver will handle this
                if (args.IsDirectory)
                {
                    return null;
                }

                var collectionType = args.GetCollectionType() ?? string.Empty;
                var accepted = new[]
                {
                    string.Empty,
                    CollectionType.HomeVideos
                };

                if (!accepted.Contains(collectionType, StringComparer.OrdinalIgnoreCase))
                {
                    return null;
                }
            }

            return base.Resolve(args);
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }
    }


}
