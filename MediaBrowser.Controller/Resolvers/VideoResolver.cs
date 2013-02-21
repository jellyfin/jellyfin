using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Controller.IO;
using System.ComponentModel.Composition;
using System.IO;

namespace MediaBrowser.Controller.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video
    /// </summary>
    [Export(typeof(IBaseItemResolver))]
    public class VideoResolver : BaseVideoResolver<Video>
    {
        public override ResolverPriority Priority
        {
            get { return ResolverPriority.Last; }
        }
    }

    /// <summary>
    /// Resolves a Path into a Video or Video subclass
    /// </summary>
    public abstract class BaseVideoResolver<T> : BaseItemResolver<T>
        where T : Video, new()
    {
        protected override T Resolve(ItemResolveEventArgs args)
        {
            // If the path is a file check for a matching extensions
            if (!args.IsDirectory)
            {
                if (FileSystemHelper.IsVideoFile(args.Path))
                {
                    VideoType type = Path.GetExtension(args.Path).EndsWith("iso", System.StringComparison.OrdinalIgnoreCase) ? VideoType.Iso : VideoType.VideoFile;

                    return new T
                    {
                        VideoType = type,
                        Path = args.Path
                    };
                }
            }

            else
            {
                // If the path is a folder, check if it's bluray or dvd
                T item = ResolveFromFolderName(args.Path);

                if (item != null)
                {
                    return item;
                }

                // Also check the subfolders for bluray or dvd
                for (int i = 0; i < args.FileSystemChildren.Length; i++)
                {
                    var folder = args.FileSystemChildren[i];

                    if (!folder.IsDirectory)
                    {
                        continue;
                    }

                    item = ResolveFromFolderName(folder.Path);

                    if (item != null)
                    {
                        return item;
                    }
                }
            }

            return null;
        }

        private T ResolveFromFolderName(string folder)
        {
            if (folder.IndexOf("video_ts", System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                return new T
                {
                    VideoType = VideoType.Dvd,
                    Path = Path.GetDirectoryName(folder)
                };
            }
            if (folder.IndexOf("bdmv", System.StringComparison.OrdinalIgnoreCase) != -1)
            {
                return new T
                {
                    VideoType = VideoType.BluRay,
                    Path = Path.GetDirectoryName(folder)
                };
            }

            return null;
        }

    }
}
