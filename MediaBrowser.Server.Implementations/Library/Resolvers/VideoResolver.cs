using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;

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
                return null;
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

    public class GenericVideoResolver<T> : BaseVideoResolver<T>
        where T : Video, new ()
    {
        public GenericVideoResolver(ILibraryManager libraryManager) : base(libraryManager)
        {
        }
    }
}
