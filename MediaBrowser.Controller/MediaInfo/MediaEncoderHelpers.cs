using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace MediaBrowser.Controller.MediaInfo
{
    /// <summary>
    /// Class MediaEncoderHelpers
    /// </summary>
    public static class MediaEncoderHelpers
    {
        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="videoPath">The video path.</param>
        /// <param name="isRemote">if set to <c>true</c> [is remote].</param>
        /// <param name="videoType">Type of the video.</param>
        /// <param name="isoType">Type of the iso.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="playableStreamFileNames">The playable stream file names.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String[][].</returns>
        public static string[] GetInputArgument(string videoPath, bool isRemote, VideoType videoType, IsoType? isoType, IIsoMount isoMount, IEnumerable<string> playableStreamFileNames, out InputType type)
        {
            var inputPath = isoMount == null ? new[] { videoPath } : new[] { isoMount.MountedPath };

            type = InputType.VideoFile;

            switch (videoType)
            {
                case VideoType.BluRay:
                    type = InputType.Bluray;
                    break;
                case VideoType.Dvd:
                    type = InputType.Dvd;
                    inputPath = GetPlayableStreamFiles(inputPath[0], playableStreamFileNames).ToArray();
                    break;
                case VideoType.Iso:
                    if (isoType.HasValue)
                    {
                        switch (isoType.Value)
                        {
                            case IsoType.BluRay:
                                type = InputType.Bluray;
                                break;
                            case IsoType.Dvd:
                                type = InputType.Dvd;
                                inputPath = GetPlayableStreamFiles(inputPath[0], playableStreamFileNames).ToArray();
                                break;
                        }
                    }
                    break;
                case VideoType.VideoFile:
                    {
                        if (isRemote)
                        {
                            type = InputType.Url;
                        }
                        break;
                    }
            }

            return inputPath;
        }

        public static List<string> GetPlayableStreamFiles(string rootPath, IEnumerable<string> filenames)
        {
            var allFiles = Directory
                .EnumerateFiles(rootPath, "*", SearchOption.AllDirectories)
                .ToList();

            return filenames.Select(name => allFiles.FirstOrDefault(f => string.Equals(Path.GetFileName(f), name, StringComparison.OrdinalIgnoreCase)))
                .Where(f => !string.IsNullOrEmpty(f))
                .ToList();
        }
        
        /// <summary>
        /// Gets the type of the input.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="videoType">Type of the video.</param>
        /// <param name="isoType">Type of the iso.</param>
        /// <returns>InputType.</returns>
        public static InputType GetInputType(string path, VideoType? videoType, IsoType? isoType)
        {
            var type = InputType.AudioFile;

            if (videoType.HasValue)
            {
                switch (videoType.Value)
                {
                    case VideoType.BluRay:
                        type = InputType.Bluray;
                        break;
                    case VideoType.Dvd:
                        type = InputType.Dvd;
                        break;
                    case VideoType.Iso:
                        if (isoType.HasValue)
                        {
                            switch (isoType.Value)
                            {
                                case IsoType.BluRay:
                                    type = InputType.Bluray;
                                    break;
                                case IsoType.Dvd:
                                    type = InputType.Dvd;
                                    break;
                            }
                        }
                        break;
                }
            }

            return type;
        }
    }
}
