#nullable disable

#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.MediaInfo;

namespace MediaBrowser.Controller.MediaEncoding
{
    /// <summary>
    /// Interface IMediaEncoder.
    /// </summary>
    public interface IMediaEncoder : ITranscoderSupport
    {
        /// <summary>
        /// Gets the encoder path.
        /// </summary>
        /// <value>The encoder path.</value>
        string EncoderPath { get; }

        /// <summary>
        /// Gets the probe path.
        /// </summary>
        /// <value>The probe path.</value>
        string ProbePath { get; }

        /// <summary>
        /// Gets the version of encoder.
        /// </summary>
        /// <returns>The version of encoder.</returns>
        Version EncoderVersion { get; }

        /// <summary>
        /// Whether p key pausing is supported.
        /// </summary>
        /// <value><c>true</c> if p key pausing is supported, <c>false</c> otherwise.</value>
        bool IsPkeyPauseSupported { get; }

        /// <summary>
        /// Gets a value indicating whether the configured Vaapi device is from AMD(radeonsi/r600 Mesa driver).
        /// </summary>
        /// <value><c>true</c> if the Vaapi device is an AMD(radeonsi/r600 Mesa driver) GPU, <c>false</c> otherwise.</value>
        bool IsVaapiDeviceAmd { get; }

        /// <summary>
        /// Gets a value indicating whether the configured Vaapi device is from Intel(iHD driver).
        /// </summary>
        /// <value><c>true</c> if the Vaapi device is an Intel(iHD driver) GPU, <c>false</c> otherwise.</value>
        bool IsVaapiDeviceInteliHD { get; }

        /// <summary>
        /// Gets a value indicating whether the configured Vaapi device is from Intel(legacy i965 driver).
        /// </summary>
        /// <value><c>true</c> if the Vaapi device is an Intel(legacy i965 driver) GPU, <c>false</c> otherwise.</value>
        bool IsVaapiDeviceInteli965 { get; }

        /// <summary>
        /// Whether given encoder codec is supported.
        /// </summary>
        /// <param name="encoder">The encoder.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SupportsEncoder(string encoder);

        /// <summary>
        /// Whether given decoder codec is supported.
        /// </summary>
        /// <param name="decoder">The decoder.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SupportsDecoder(string decoder);

        /// <summary>
        /// Whether given hardware acceleration type is supported.
        /// </summary>
        /// <param name="hwaccel">The hwaccel.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        bool SupportsHwaccel(string hwaccel);

        /// <summary>
        /// Whether given filter is supported.
        /// </summary>
        /// <param name="filter">The filter.</param>
        /// <returns><c>true</c> if the filter is supported, <c>false</c> otherwise.</returns>
        bool SupportsFilter(string filter);

        /// <summary>
        /// Whether filter is supported with the given option.
        /// </summary>
        /// <param name="option">The option.</param>
        /// <returns><c>true</c> if the filter is supported, <c>false</c> otherwise.</returns>
        bool SupportsFilterWithOption(FilterOptionType option);

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
        /// <param name="inputFile">Input file.</param>
        /// <param name="container">Video container type.</param>
        /// <param name="mediaSource">Media source information.</param>
        /// <param name="videoStream">Media stream information.</param>
        /// <param name="threedFormat">Video 3D format.</param>
        /// <param name="offset">Time offset.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>Location of video image.</returns>
        Task<string> ExtractVideoImage(string inputFile, string container, MediaSourceInfo mediaSource, MediaStream videoStream, Video3DFormat? threedFormat, TimeSpan? offset, CancellationToken cancellationToken);

        /// <summary>
        /// Extracts the video image.
        /// </summary>
        /// <param name="inputFile">Input file.</param>
        /// <param name="container">Video container type.</param>
        /// <param name="mediaSource">Media source information.</param>
        /// <param name="imageStream">Media stream information.</param>
        /// <param name="imageStreamIndex">Index of the stream to extract from.</param>
        /// <param name="targetFormat">The format of the file to write.</param>
        /// <param name="cancellationToken">CancellationToken to use for operation.</param>
        /// <returns>Location of video image.</returns>
        Task<string> ExtractVideoImage(string inputFile, string container, MediaSourceInfo mediaSource, MediaStream imageStream, int? imageStreamIndex, ImageFormat? targetFormat, CancellationToken cancellationToken);

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
        /// <param name="inputFile">The input file.</param>
        /// <param name="mediaSource">The mediaSource.</param>
        /// <returns>System.String.</returns>
        string GetInputArgument(string inputFile, MediaSourceInfo mediaSource);

        /// <summary>
        /// Gets the input argument for an external subtitle file.
        /// </summary>
        /// <param name="inputFile">The input file.</param>
        /// <returns>System.String.</returns>
        string GetExternalSubtitleInputArgument(string inputFile);

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

        /// <summary>
        /// Sets the path to find FFmpeg.
        /// </summary>
        void SetFFmpegPath();

        /// <summary>
        /// Updates the encoder path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <param name="pathType">The type of path.</param>
        void UpdateEncoderPath(string path, string pathType);

        /// <summary>
        /// Gets the primary playlist of .vob files.
        /// </summary>
        /// <param name="path">The to the .vob files.</param>
        /// <param name="titleNumber">The title number to start with.</param>
        /// <returns>A playlist.</returns>
        IEnumerable<string> GetPrimaryPlaylistVobFiles(string path, uint? titleNumber);
    }
}
