using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;

namespace MediaBrowser.Controller.Resolvers
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

    /// <summary>
    /// Resolves a Path into a Video or Video subclass
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseVideoResolver<T> : BaseItemResolver<T>
        where T : Video, new()
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>`0.</returns>
        protected override T Resolve(ItemResolveArgs args)
        {
            // If the path is a file check for a matching extensions
            if (!args.IsDirectory)
            {
                if (EntityResolutionHelper.IsVideoFile(args.Path))
                {
                    var extension = Path.GetExtension(args.Path);

                    var type = string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".img", StringComparison.OrdinalIgnoreCase) ? 
                        VideoType.Iso : VideoType.VideoFile;

                    return new T
                    {
                        VideoType = type,
                        Path = args.Path
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Sets the initial item values.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="args">The args.</param>
        protected override void SetInitialItemValues(T item, ItemResolveArgs args)
        {
            base.SetInitialItemValues(item, args);

            item.VideoFormat = item.Path.IndexOf("[3d]", StringComparison.OrdinalIgnoreCase) != -1 ? VideoFormat.Digital3D : item.Path.IndexOf("[sbs3d]", StringComparison.OrdinalIgnoreCase) != -1 ? VideoFormat.Sbs3D : VideoFormat.Standard;
        }
    }
}
