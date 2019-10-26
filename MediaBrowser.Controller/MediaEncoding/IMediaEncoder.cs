using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.MediaInfo;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Interface IMediaEncoder
    /// </summary>
    public interface IMediaEncoder : ITranscoderSupport
    {
        /// <summary>
        /// The location of the discovered FFmpeg tool.
        /// </summary>
        FFmpegLocation EncoderLocation { get; }

        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        string EncoderPath { get; }

        /// <summary>
        /// Supportses the decoder.
        /// </summary>
        /// <param name="decoder">The decoder.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SupportsDecoder(string decoder);

        /// <summary>
        /// Extracts the audio image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="imageStreamIndex">Index of the image stream.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<string> ExtractAudioImage(string path, int? imageStreamIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video image.
        /// </summary>
        Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, MediaStream videoStream, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken);

        Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, MediaStream imageStream, int? imageStreamIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video images on interval.
        /// </summary>
        Task ExtractVideoImagesOnInterval(string[] inputFiles,
            string container,
            MediaStream videoStream,
            MediaProtocol protocol,
            Video3DFormat? threedFormat,
            TimeSpan interval,
            string targetDirectory,
            string filenamePrefix,
            int? maxWidth,
            CancellationToken cancellationToken);

        /// <summary>
        /// Gets the media info.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<MediaInfo> GetMediaInfo(MediaInfoRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        string GetInputArgument(IReadOnlyList<string> inputFiles, MediaProtocol protocol);

        /// <summary>
        /// Gets the time parameter.
        /// </summary>
        /// <param name="ticks">The ticks.</param>
        /// <returns>System.String.</returns>
        string GetTimeParameter(long ticks);

        Task ConvertImage(string inputPath, string outputPath);

        /// <summary>
        /// Escapes the subtitle filter path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string EscapeSubtitleFilterPath(string path);

        void SetFFmpegPath();

        void UpdateEncoderPath(string path, string pathType);
        bool SupportsEncoder(string encoder);

        IEnumerable<string> GetPrimaryPlaylistVobFiles(string path, IIsoMount isoMount, uint? titleNumber);
    }
}
