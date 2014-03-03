using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using System;
using System.IO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video or Video subclass
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseVideoResolver<T> : ItemResolver<T>
        where T : Video, new()
    {
        /// <summary>
        /// Resolves the specified args.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <returns>`0.</returns>
        protected override T Resolve(ItemResolveArgs args)
        {
            return ResolveVideo<T>(args);
        }

        /// <summary>
        /// Resolves the video.
        /// </summary>
        /// <typeparam name="TVideoType">The type of the T video type.</typeparam>
        /// <param name="args">The args.</param>
        /// <returns>``0.</returns>
        protected TVideoType ResolveVideo<TVideoType>(ItemResolveArgs args)
              where TVideoType : Video, new()
        {
            // If the path is a file check for a matching extensions
            if (!args.IsDirectory)
            {
                var isPlaceHolder = EntityResolutionHelper.IsVideoPlaceHolder(args.Path);

                if (EntityResolutionHelper.IsVideoFile(args.Path) || isPlaceHolder)
                {
                    var extension = Path.GetExtension(args.Path);

                    var type = string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".img", StringComparison.OrdinalIgnoreCase) ?
                        VideoType.Iso : VideoType.VideoFile;

                    return new TVideoType
                    {
                        VideoType = type,
                        Path = args.Path,
                        IsInMixedFolder = true,
                        IsPlaceHolder = isPlaceHolder
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

            if (item.Path.IndexOf("[3d]", StringComparison.OrdinalIgnoreCase) != -1 || item.Path.IndexOf("[sbs3d]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                item.Video3DFormat = Video3DFormat.HalfSideBySide;
            }
            else if (item.Path.IndexOf("[hsbs]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                item.Video3DFormat = Video3DFormat.HalfSideBySide;
            }
            else if (item.Path.IndexOf("[fsbs]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                item.Video3DFormat = Video3DFormat.FullSideBySide;
            }
            else if (item.Path.IndexOf("[ftab]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                item.Video3DFormat = Video3DFormat.FullTopAndBottom;
            }
            else if (item.Path.IndexOf("[htab]", StringComparison.OrdinalIgnoreCase) != -1)
            {
                item.Video3DFormat = Video3DFormat.HalfTopAndBottom;
            }
        }
    }
}
