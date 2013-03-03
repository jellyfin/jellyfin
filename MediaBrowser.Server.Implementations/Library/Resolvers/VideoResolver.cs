using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Resolvers;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video
    /// </summary>
    public class VideoResolver : BaseVideoResolver<Video>
    {
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
