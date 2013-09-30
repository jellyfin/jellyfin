using MediaBrowser.Common.MediaInfo;
using MediaBrowser.Controller.Entities;
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
        /// <param name="video">The video.</param>
        /// <param name="isoMount">The iso mount.</param>
        /// <param name="type">The type.</param>
        /// <returns>System.String[][].</returns>
        public static string[] GetInputArgument(Video video, IIsoMount isoMount, out InputType type)
        {
            var inputPath = isoMount == null ? new[] { video.Path } : new[] { isoMount.MountedPath };

            type = InputType.VideoFile;

            switch (video.VideoType)
            {
                case VideoType.BluRay:
                    type = InputType.Bluray;
                    break;
                case VideoType.Dvd:
                    type = InputType.Dvd;
                    inputPath = video.GetPlayableStreamFiles(inputPath[0]).ToArray();
                    break;
                case VideoType.Iso:
                    if (video.IsoType.HasValue)
                    {
                        switch (video.IsoType.Value)
                        {
                            case IsoType.BluRay:
                                type = InputType.Bluray;
                                break;
                            case IsoType.Dvd:
                                type = InputType.Dvd;
                                inputPath = video.GetPlayableStreamFiles(inputPath[0]).ToArray();
                                break;
                        }
                    }
                    break;
                case VideoType.VideoFile:
                    {
                        if (video.LocationType == LocationType.Remote)
                        {
                            type = InputType.Url;
                        }
                        break;
                    }
            }

            return inputPath;
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
