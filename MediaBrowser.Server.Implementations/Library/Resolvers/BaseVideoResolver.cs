using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Entities;
using MediaBrowser.Naming.Video;
using MediaBrowser.Server.Implementations.Logging;
using System;
using System.IO;

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
            return ResolveVideo<T>(args, false);
        }

        /// <summary>
        /// Resolves the video.
        /// </summary>
        /// <typeparam name="TVideoType">The type of the T video type.</typeparam>
        /// <param name="args">The args.</param>
        /// <param name="parseName">if set to <c>true</c> [parse name].</param>
        /// <returns>``0.</returns>
        protected TVideoType ResolveVideo<TVideoType>(ItemResolveArgs args, bool parseName)
              where TVideoType : Video, new()
        {
            var namingOptions = ((LibraryManager)LibraryManager).GetNamingOptions();
            
            // If the path is a file check for a matching extensions
            var parser = new Naming.Video.VideoResolver(namingOptions, new PatternsLogger());

            if (args.IsDirectory)
            {
                TVideoType video = null;
                VideoFileInfo videoInfo = null;

                // Loop through each child file/folder and see if we find a video
                foreach (var child in args.FileSystemChildren)
                {
                    var filename = child.Name;

                    if ((child.Attributes & FileAttributes.Directory) == FileAttributes.Directory)
                    {
                        if (IsDvdDirectory(filename))
                        {
                            videoInfo = parser.ResolveDirectory(args.Path);

                            if (videoInfo == null)
                            {
                                return null;
                            }

                            video = new TVideoType
                            {
                                Path = args.Path,
                                VideoType = VideoType.Dvd,
                                ProductionYear = videoInfo.Year
                            };
                            break;
                        }
                        if (IsBluRayDirectory(filename))
                        {
                            videoInfo = parser.ResolveDirectory(args.Path);

                            if (videoInfo == null)
                            {
                                return null;
                            }

                            video = new TVideoType
                            {
                                Path = args.Path,
                                VideoType = VideoType.BluRay,
                                ProductionYear = videoInfo.Year
                            };
                            break;
                        }
                    }
                    else if (IsDvdFile(filename))
                    {
                        videoInfo = parser.ResolveDirectory(args.Path);

                        if (videoInfo == null)
                        {
                            return null;
                        }

                        video = new TVideoType
                        {
                            Path = args.Path,
                            VideoType = VideoType.Dvd,
                            ProductionYear = videoInfo.Year
                        };
                        break;
                    }
                }

                if (video != null)
                {
                    video.Name = parseName ? 
                        videoInfo.Name :
                        Path.GetFileName(args.Path);

                    Set3DFormat(video, videoInfo);
                }

                return video;
            }
            else
            {
                var videoInfo = parser.Resolve(args.Path, false, false);

                if (videoInfo == null)
                {
                    return null;
                }

                if (LibraryManager.IsVideoFile(args.Path, args.GetLibraryOptions()) || videoInfo.IsStub)
                {
                    var path = args.Path;

                    var video = new TVideoType
                    {
                        Path = path,
                        IsInMixedFolder = true,
                        ProductionYear = videoInfo.Year
                    };

                    SetVideoType(video, videoInfo);
                    
                    video.Name = parseName ?
                        videoInfo.Name :
                        Path.GetFileNameWithoutExtension(args.Path);

                    Set3DFormat(video, videoInfo);

                    return video;
                }
            }

            return null;
        }

        protected void SetVideoType(Video video, VideoFileInfo videoInfo)
        {
            var extension = Path.GetExtension(video.Path);
            video.VideoType = string.Equals(extension, ".iso", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".img", StringComparison.OrdinalIgnoreCase) ?
              VideoType.Iso :
              VideoType.VideoFile;

            video.IsShortcut = string.Equals(extension, ".strm", StringComparison.OrdinalIgnoreCase);
            video.IsPlaceHolder = videoInfo.IsStub;

            if (videoInfo.IsStub)
            {
                if (string.Equals(videoInfo.StubType, "dvd", StringComparison.OrdinalIgnoreCase))
                {
                    video.VideoType = VideoType.Dvd;
                }
                else if (string.Equals(videoInfo.StubType, "hddvd", StringComparison.OrdinalIgnoreCase))
                {
                    video.VideoType = VideoType.HdDvd;
                    video.IsHD = true;
                }
                else if (string.Equals(videoInfo.StubType, "bluray", StringComparison.OrdinalIgnoreCase))
                {
                    video.VideoType = VideoType.BluRay;
                    video.IsHD = true;
                }
                else if (string.Equals(videoInfo.StubType, "hdtv", StringComparison.OrdinalIgnoreCase))
                {
                    video.IsHD = true;
                }
            }

            SetIsoType(video);
        }

        protected void SetIsoType(Video video)
        {
            if (video.VideoType == VideoType.Iso)
            {
                if (video.Path.IndexOf("dvd", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    video.IsoType = IsoType.Dvd;
                }
                else if (video.Path.IndexOf("bluray", StringComparison.OrdinalIgnoreCase) != -1)
                {
                    video.IsoType = IsoType.BluRay;
                }
            }
        }

        protected void Set3DFormat(Video video, bool is3D, string format3D)
        {
            if (is3D)
            {
                if (string.Equals(format3D, "fsbs", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.FullSideBySide;
                }
                else if (string.Equals(format3D, "ftab", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.FullTopAndBottom;
                }
                else if (string.Equals(format3D, "hsbs", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.HalfSideBySide;
                }
                else if (string.Equals(format3D, "htab", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                }
                else if (string.Equals(format3D, "sbs", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.HalfSideBySide;
                }
                else if (string.Equals(format3D, "sbs3d", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.HalfSideBySide;
                }
                else if (string.Equals(format3D, "tab", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.HalfTopAndBottom;
                }
                else if (string.Equals(format3D, "mvc", StringComparison.OrdinalIgnoreCase))
                {
                    video.Video3DFormat = Video3DFormat.MVC;
                }
            }
        }

        protected void Set3DFormat(Video video, VideoFileInfo videoInfo)
        {
            Set3DFormat(video, videoInfo.Is3D, videoInfo.Format3D);
        }

        protected void Set3DFormat(Video video)
        {
            var namingOptions = ((LibraryManager)LibraryManager).GetNamingOptions();

            var resolver = new Format3DParser(namingOptions, new PatternsLogger());
            var result = resolver.Parse(video.Path);

            Set3DFormat(video, result.Is3D, result.Format3D);
        }

        /// <summary>
        /// Determines whether [is DVD directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is DVD directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        protected bool IsDvdDirectory(string directoryName)
        {
            return string.Equals(directoryName, "video_ts", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether [is DVD file] [the specified name].
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns><c>true</c> if [is DVD file] [the specified name]; otherwise, <c>false</c>.</returns>
        protected bool IsDvdFile(string name)
        {
            return string.Equals(name, "video_ts.ifo", StringComparison.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// Determines whether [is blu ray directory] [the specified directory name].
        /// </summary>
        /// <param name="directoryName">Name of the directory.</param>
        /// <returns><c>true</c> if [is blu ray directory] [the specified directory name]; otherwise, <c>false</c>.</returns>
        protected bool IsBluRayDirectory(string directoryName)
        {
            return string.Equals(directoryName, "bdmv", StringComparison.OrdinalIgnoreCase);
        }
    }
}
