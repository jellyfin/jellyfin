#nullable disable

using Emby.Naming.Common;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Resolvers;
using MediaBrowser.Model.Entities;

namespace Emby.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video or Video subclass.
    /// </summary>
    public class VideoExtraResolver : BaseVideoResolver<Video>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="VideoExtraResolver"/> class.
        /// </summary>
        /// <param name="namingOptions">The naming options.</param>
        public VideoExtraResolver(NamingOptions namingOptions)
            : base(namingOptions)
        {
        }

        /// <summary>
        /// Gets the priority.
        /// </summary>
        /// <value>The priority.</value>
        public override ResolverPriority Priority => ResolverPriority.Last;

        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>The video extra or null if not handled by this resolver.</returns>
        public override Video Resolve(ItemResolveArgs args)
        {
            // Only handle owned items
            if (args.Parent != null)
            {
                return null;
            }

            var ownedItem = base.Resolve(args);

            // Re-resolve items that have their own type
            if (ownedItem.ExtraType == ExtraType.Trailer)
            {
                ownedItem = ResolveVideo<Trailer>(args, false);
            }

            return ownedItem;
        }
    }
}
