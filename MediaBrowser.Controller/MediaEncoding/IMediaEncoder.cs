using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Interface IMediaEncoder
    /// </summary>
    public interface IMediaEncoder
    {
        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        string EncoderPath { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>The version.</value>
        string Version { get; }

        /// <summary>
        /// Extracts the audio image.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> ExtractAudioImage(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video image.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="threedFormat">The threed format.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task{Stream}.</returns>
        Task<Stream> ExtractVideoImage(string[] inputFiles, MediaProtocol protocol, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken);

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
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <param name="isAudio">if set to <c>true</c> [is audio].</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        Task<InternalMediaInfoResult> GetMediaInfo(string[] inputFiles, MediaProtocol protocol, bool isAudio, CancellationToken cancellationToken);

        /// <summary>
        /// Gets the probe size argument.
        /// </summary>
        /// <param name="inputFiles">The input files.</param>
        /// <param name="protocol">The protocol.</param>
        /// <returns>System.String.</returns>
        string GetProbeSizeArgument(string[] inputFiles, MediaProtocol protocol);

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
    }
}
