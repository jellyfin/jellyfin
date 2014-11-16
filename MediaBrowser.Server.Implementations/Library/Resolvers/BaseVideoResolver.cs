using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Naming.Audio;
using MediaBrowser.Naming.Video;
using System;

namespace MediaBrowser.Server.Implementations.Library.Resolvers
{
    /// <summary>
    /// Resolves a Path into a Video or Video subclass
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class BaseVideoResolver<T> : Controller.Resolvers.ItemResolver<T>
        where T : Video, new()
    {
        protected readonly ILibraryManager LibraryManager;

        protected BaseVideoResolver(ILibraryManager libraryManager)
        {
            LibraryManager = libraryManager;
        }

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
                var parser = new Naming.Video.VideoResolver(new ExpandedVideoOptions(), new AudioOptions(), new Naming.Logging.NullLogger());
                var videoInfo = parser.ResolveFile(args.Path);

                if (videoInfo == null)
                {
                    return null;
                }

                var isShortcut = string.Equals(videoInfo.Container, "strm", StringComparison.OrdinalIgnoreCase);

                if (LibraryManager.IsVideoFile(args.Path) || videoInfo.IsStub || isShortcut)
                {
                    var type = string.Equals(videoInfo.Container, "iso", StringComparison.OrdinalIgnoreCase) || string.Equals(videoInfo.Container, "img", StringComparison.OrdinalIgnoreCase) ?
                        VideoType.Iso : 
                        VideoType.VideoFile;

                    var path = args.Path;

                    var video = new TVideoType
                    {
                        VideoType = type,
                        Path = path,
                        IsInMixedFolder = true,
                        IsPlaceHolder = videoInfo.IsStub,
                        IsShortcut = isShortcut,
                        Name = videoInfo.Name,
                        ProductionYear = videoInfo.Year
                    };

                    if (videoInfo.IsStub)
                    {
                        if (string.Equals(videoInfo.StubType, "dvd", StringComparison.OrdinalIgnoreCase))
                        {
                            video.VideoType = VideoType.Dvd;
                        }
                        else if (string.Equals(videoInfo.StubType, "hddvd", StringComparison.OrdinalIgnoreCase))
                        {
                            video.VideoType = VideoType.HdDvd;
                        }
                        else if (string.Equals(videoInfo.StubType, "bluray", StringComparison.OrdinalIgnoreCase))
                        {
                            video.VideoType = VideoType.BluRay;
                        }
                    }

                    return video;
                }
            }

            return null;
        }
    }
}
