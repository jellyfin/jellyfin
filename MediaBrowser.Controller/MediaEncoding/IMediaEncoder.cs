using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Interface IMediaEncoder
    /// </summary>
    public interface IMediaEncoder : ITranscoderSupport
    {
        string EncoderLocationType { get; }

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
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken);

        Task<string> ExtractVideoImage(string[] inputFiles, string container, MediaProtocol protocol, int? imageStreamIndex, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video images on interval.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="interval">The interval.</param>
        /// <param name="targetDirectory">The target directory.</param>
        /// <param name="filenamePrefix">The filename prefix.</param>
        /// <param name="maxWidth">The maximum width.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task ExtractVideoImagesOnInterval(string[] inputFiles,
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
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        string GetProbeSizeAndAnalyzeDurationArgument(string[] inputFiles, MediaProtocol protocol);

        /// <summary>
        /// Gets the input argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        string GetInputArgument(string[] inputFiles, MediaProtocol protocol);

        /// <summary>
        /// Gets the time parameter.
        /// </summary>
        /// <param name="ticks">The ticks.</param>
        /// <returns>System.String.</returns>
        string GetTimeParameter(long ticks);

        /// <summary>
        /// Encodes the audio.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<string> EncodeAudio(EncodingJobOptions options,
            IProgress<double> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Encodes the video.
        /// </summary>
        /// <param name="options">The options.</param>
        /// <param name="progress">The progress.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task&lt;System.String&gt;.</returns>
        Task<string> EncodeVideo(EncodingJobOptions options,
            IProgress<double> progress,
            CancellationToken cancellationToken);

        /// <summary>
        /// Escapes the subtitle filter path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>System.String.</returns>
        string EscapeSubtitleFilterPath(string path);

        Task Init();

        Task UpdateEncoderPath(string path, string pathType);
        bool SupportsEncoder(string encoder);
        bool IsDefaultEncoderPath { get; }
    }
}
