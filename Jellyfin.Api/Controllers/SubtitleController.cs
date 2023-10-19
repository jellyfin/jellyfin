using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Attributes;
using Jellyfin.Api.Constants;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Models.SubtitleDtos;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Controller.Subtitles;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Providers;
using MediaBrowser.Model.Subtitles;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Subtitle controller.
/// </summary>
[Route("")]
public class SubtitleController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _serverConfigurationManager;
    private readonly ILibraryManager _libraryManager;
    private readonly ISubtitleManager _subtitleManager;
    private readonly ISubtitleEncoder _subtitleEncoder;
    private readonly IMediaSourceManager _mediaSourceManager;
    private readonly IProviderManager _providerManager;
    private readonly IFileSystem _fileSystem;
    private readonly ILogger<SubtitleController> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SubtitleController"/> class.
    /// </summary>
    /// <param name="serverConfigurationManager">Instance of <see cref="IServerConfigurationManager"/> interface.</param>
    /// <param name="libraryManager">Instance of <see cref="ILibraryManager"/> interface.</param>
    /// <param name="subtitleManager">Instance of <see cref="ISubtitleManager"/> interface.</param>
    /// <param name="subtitleEncoder">Instance of <see cref="ISubtitleEncoder"/> interface.</param>
    /// <param name="mediaSourceManager">Instance of <see cref="IMediaSourceManager"/> interface.</param>
    /// <param name="providerManager">Instance of <see cref="IProviderManager"/> interface.</param>
    /// <param name="fileSystem">Instance of <see cref="IFileSystem"/> interface.</param>
    /// <param name="logger">Instance of <see cref="ILogger{SubtitleController}"/> interface.</param>
    public SubtitleController(
        IServerConfigurationManager serverConfigurationManager,
        ILibraryManager libraryManager,
        ISubtitleManager subtitleManager,
        ISubtitleEncoder subtitleEncoder,
        IMediaSourceManager mediaSourceManager,
        IProviderManager providerManager,
        IFileSystem fileSystem,
        ILogger<SubtitleController> logger)
    {
        _serverConfigurationManager = serverConfigurationManager;
        _libraryManager = libraryManager;
        _subtitleManager = subtitleManager;
        _subtitleEncoder = subtitleEncoder;
        _mediaSourceManager = mediaSourceManager;
        _providerManager = providerManager;
        _fileSystem = fileSystem;
        _logger = logger;
    }

    /// <summary>
    /// Deletes an external subtitle file.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="index">The index of the subtitle file.</param>
    /// <response code="204">Subtitle deleted.</response>
    /// <response code="404">Item not found.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpDelete("Videos/{itemId}/Subtitles/{index}")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult> DeleteSubtitle(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int index)
    {
        var item = _libraryManager.GetItemById(itemId);

        if (item is null)
        {
            return NotFound();
        }

        await _subtitleManager.DeleteSubtitles(item, index).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>
    /// Search remote subtitles.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="language">The language of the subtitles.</param>
    /// <param name="isPerfectMatch">Optional. Only show subtitles which are a perfect match.</param>
    /// <response code="200">Subtitles retrieved.</response>
    /// <returns>An array of <see cref="RemoteSubtitleInfo"/>.</returns>
    [HttpGet("Items/{itemId}/RemoteSearch/Subtitles/{language}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RemoteSubtitleInfo>>> SearchRemoteSubtitles(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string language,
        [FromQuery] bool? isPerfectMatch)
    {
        var video = (Video)_libraryManager.GetItemById(itemId);

        return await _subtitleManager.SearchSubtitles(video, language, isPerfectMatch, false, CancellationToken.None).ConfigureAwait(false);
    }

    /// <summary>
    /// Downloads a remote subtitle.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="subtitleId">The subtitle id.</param>
    /// <response code="204">Subtitle downloaded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Items/{itemId}/RemoteSearch/Subtitles/{subtitleId}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> DownloadRemoteSubtitles(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] string subtitleId)
    {
        var video = (Video)_libraryManager.GetItemById(itemId);

        try
        {
            await _subtitleManager.DownloadSubtitles(video, subtitleId, CancellationToken.None)
                .ConfigureAwait(false);

            _providerManager.QueueRefresh(video.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading subtitles");
        }

        return NoContent();
    }

    /// <summary>
    /// Gets the remote subtitles.
    /// </summary>
    /// <param name="id">The item id.</param>
    /// <response code="200">File returned.</response>
    /// <returns>A <see cref="FileStreamResult"/> with the subtitle file.</returns>
    [HttpGet("Providers/Subtitles/Subtitles/{id}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [Produces(MediaTypeNames.Application.Octet)]
    [ProducesFile("text/*")]
    public async Task<ActionResult> GetRemoteSubtitles([FromRoute, Required] string id)
    {
        var result = await _subtitleManager.GetRemoteSubtitles(id, CancellationToken.None).ConfigureAwait(false);

        return File(result.Stream, MimeTypes.GetMimeType("file." + result.Format));
    }

    /// <summary>
    /// Gets subtitles in a specified format.
    /// </summary>
    /// <param name="routeItemId">The (route) item id.</param>
    /// <param name="routeMediaSourceId">The (route) media source id.</param>
    /// <param name="routeIndex">The (route) subtitle stream index.</param>
    /// <param name="routeFormat">The (route) format of the returned subtitle.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="index">The subtitle stream index.</param>
    /// <param name="format">The format of the returned subtitle.</param>
    /// <param name="endPositionTicks">Optional. The end position of the subtitle in ticks.</param>
    /// <param name="copyTimestamps">Optional. Whether to copy the timestamps.</param>
    /// <param name="addVttTimeMap">Optional. Whether to add a VTT time map.</param>
    /// <param name="startPositionTicks">The start position of the subtitle in ticks.</param>
    /// <response code="200">File returned.</response>
    /// <returns>A <see cref="FileContentResult"/> with the subtitle file.</returns>
    [HttpGet("Videos/{routeItemId}/{routeMediaSourceId}/Subtitles/{routeIndex}/Stream.{routeFormat}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile("text/*")]
    public async Task<ActionResult> GetSubtitle(
        [FromRoute, Required] Guid routeItemId,
        [FromRoute, Required] string routeMediaSourceId,
        [FromRoute, Required] int routeIndex,
        [FromRoute, Required] string routeFormat,
        [FromQuery, ParameterObsolete] Guid? itemId,
        [FromQuery, ParameterObsolete] string? mediaSourceId,
        [FromQuery, ParameterObsolete] int? index,
        [FromQuery, ParameterObsolete] string? format,
        [FromQuery] long? endPositionTicks,
        [FromQuery] bool copyTimestamps = false,
        [FromQuery] bool addVttTimeMap = false,
        [FromQuery] long startPositionTicks = 0)
    {
        // Set parameters to route value if not provided via query.
        itemId ??= routeItemId;
        mediaSourceId ??= routeMediaSourceId;
        index ??= routeIndex;
        format ??= routeFormat;

        if (string.Equals(format, "js", StringComparison.OrdinalIgnoreCase))
        {
            format = "json";
        }

        if (string.IsNullOrEmpty(format))
        {
            var item = (Video)_libraryManager.GetItemById(itemId.Value);

            var idString = itemId.Value.ToString("N", CultureInfo.InvariantCulture);
            var mediaSource = _mediaSourceManager.GetStaticMediaSources(item, false)
                .First(i => string.Equals(i.Id, mediaSourceId ?? idString, StringComparison.Ordinal));

            var subtitleStream = mediaSource.MediaStreams
                .First(i => i.Type == MediaStreamType.Subtitle && i.Index == index);

            return PhysicalFile(subtitleStream.Path, MimeTypes.GetMimeType(subtitleStream.Path));
        }

        if (string.Equals(format, "vtt", StringComparison.OrdinalIgnoreCase) && addVttTimeMap)
        {
            Stream stream = await EncodeSubtitles(itemId.Value, mediaSourceId, index.Value, format, startPositionTicks, endPositionTicks, copyTimestamps).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                using var reader = new StreamReader(stream);

                var text = await reader.ReadToEndAsync().ConfigureAwait(false);

                text = text.Replace("WEBVTT", "WEBVTT\nX-TIMESTAMP-MAP=MPEGTS:900000,LOCAL:00:00:00.000", StringComparison.Ordinal);

                return File(Encoding.UTF8.GetBytes(text), MimeTypes.GetMimeType("file." + format));
            }
        }

        return File(
            await EncodeSubtitles(
                itemId.Value,
                mediaSourceId,
                index.Value,
                format,
                startPositionTicks,
                endPositionTicks,
                copyTimestamps).ConfigureAwait(false),
            MimeTypes.GetMimeType("file." + format));
    }

    /// <summary>
    /// Gets subtitles in a specified format.
    /// </summary>
    /// <param name="routeItemId">The (route) item id.</param>
    /// <param name="routeMediaSourceId">The (route) media source id.</param>
    /// <param name="routeIndex">The (route) subtitle stream index.</param>
    /// <param name="routeStartPositionTicks">The (route) start position of the subtitle in ticks.</param>
    /// <param name="routeFormat">The (route) format of the returned subtitle.</param>
    /// <param name="itemId">The item id.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="index">The subtitle stream index.</param>
    /// <param name="startPositionTicks">The start position of the subtitle in ticks.</param>
    /// <param name="format">The format of the returned subtitle.</param>
    /// <param name="endPositionTicks">Optional. The end position of the subtitle in ticks.</param>
    /// <param name="copyTimestamps">Optional. Whether to copy the timestamps.</param>
    /// <param name="addVttTimeMap">Optional. Whether to add a VTT time map.</param>
    /// <response code="200">File returned.</response>
    /// <returns>A <see cref="FileContentResult"/> with the subtitle file.</returns>
    [HttpGet("Videos/{routeItemId}/{routeMediaSourceId}/Subtitles/{routeIndex}/{routeStartPositionTicks}/Stream.{routeFormat}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile("text/*")]
    public Task<ActionResult> GetSubtitleWithTicks(
        [FromRoute, Required] Guid routeItemId,
        [FromRoute, Required] string routeMediaSourceId,
        [FromRoute, Required] int routeIndex,
        [FromRoute, Required] long routeStartPositionTicks,
        [FromRoute, Required] string routeFormat,
        [FromQuery, ParameterObsolete] Guid? itemId,
        [FromQuery, ParameterObsolete] string? mediaSourceId,
        [FromQuery, ParameterObsolete] int? index,
        [FromQuery, ParameterObsolete] long? startPositionTicks,
        [FromQuery, ParameterObsolete] string? format,
        [FromQuery] long? endPositionTicks,
        [FromQuery] bool copyTimestamps = false,
        [FromQuery] bool addVttTimeMap = false)
    {
        return GetSubtitle(
            routeItemId,
            routeMediaSourceId,
            routeIndex,
            routeFormat,
            itemId,
            mediaSourceId,
            index,
            format,
            endPositionTicks,
            copyTimestamps,
            addVttTimeMap,
            startPositionTicks ?? routeStartPositionTicks);
    }

    /// <summary>
    /// Gets an HLS subtitle playlist.
    /// </summary>
    /// <param name="itemId">The item id.</param>
    /// <param name="index">The subtitle stream index.</param>
    /// <param name="mediaSourceId">The media source id.</param>
    /// <param name="segmentLength">The subtitle segment length.</param>
    /// <response code="200">Subtitle playlist retrieved.</response>
    /// <returns>A <see cref="FileContentResult"/> with the HLS subtitle playlist.</returns>
    [HttpGet("Videos/{itemId}/{mediaSourceId}/Subtitles/{index}/subtitles.m3u8")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesPlaylistFile]
    [SuppressMessage("Microsoft.Performance", "CA1801:ReviewUnusedParameters", MessageId = "index", Justification = "Imported from ServiceStack")]
    public async Task<ActionResult> GetSubtitlePlaylist(
        [FromRoute, Required] Guid itemId,
        [FromRoute, Required] int index,
        [FromRoute, Required] string mediaSourceId,
        [FromQuery, Required] int segmentLength)
    {
        var item = (Video)_libraryManager.GetItemById(itemId);

        var mediaSource = await _mediaSourceManager.GetMediaSource(item, mediaSourceId, null, false, CancellationToken.None).ConfigureAwait(false);

        var runtime = mediaSource.RunTimeTicks ?? -1;

        if (runtime <= 0)
        {
            throw new ArgumentException("HLS Subtitles are not supported for this media.");
        }

        var segmentLengthTicks = TimeSpan.FromSeconds(segmentLength).Ticks;
        if (segmentLengthTicks <= 0)
        {
            throw new ArgumentException("segmentLength was not given, or it was given incorrectly. (It should be bigger than 0)");
        }

        var builder = new StringBuilder();
        builder.AppendLine("#EXTM3U")
            .Append("#EXT-X-TARGETDURATION:")
            .Append(segmentLength)
            .AppendLine()
            .AppendLine("#EXT-X-VERSION:3")
            .AppendLine("#EXT-X-MEDIA-SEQUENCE:0")
            .AppendLine("#EXT-X-PLAYLIST-TYPE:VOD");

        long positionTicks = 0;

        var accessToken = User.GetToken();

        while (positionTicks < runtime)
        {
            var remaining = runtime - positionTicks;
            var lengthTicks = Math.Min(remaining, segmentLengthTicks);

            builder.Append("#EXTINF:")
                .Append(TimeSpan.FromTicks(lengthTicks).TotalSeconds)
                .Append(',')
                .AppendLine();

            var endPositionTicks = Math.Min(runtime, positionTicks + segmentLengthTicks);

            var url = string.Format(
                CultureInfo.InvariantCulture,
                "stream.vtt?CopyTimestamps=true&AddVttTimeMap=true&StartPositionTicks={0}&EndPositionTicks={1}&api_key={2}",
                positionTicks.ToString(CultureInfo.InvariantCulture),
                endPositionTicks.ToString(CultureInfo.InvariantCulture),
                accessToken);

            builder.AppendLine(url);

            positionTicks += segmentLengthTicks;
        }

        builder.AppendLine("#EXT-X-ENDLIST");
        return File(Encoding.UTF8.GetBytes(builder.ToString()), MimeTypes.GetMimeType("playlist.m3u8"));
    }

    /// <summary>
    /// Upload an external subtitle file.
    /// </summary>
    /// <param name="itemId">The item the subtitle belongs to.</param>
    /// <param name="body">The request body.</param>
    /// <response code="204">Subtitle uploaded.</response>
    /// <returns>A <see cref="NoContentResult"/>.</returns>
    [HttpPost("Videos/{itemId}/Subtitles")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<ActionResult> UploadSubtitle(
        [FromRoute, Required] Guid itemId,
        [FromBody, Required] UploadSubtitleDto body)
    {
        var video = (Video)_libraryManager.GetItemById(itemId);
        var stream = new CryptoStream(Request.Body, new FromBase64Transform(), CryptoStreamMode.Read);
        await using (stream.ConfigureAwait(false))
        {
            await _subtitleManager.UploadSubtitle(
                video,
                new SubtitleResponse
                {
                    Format = body.Format,
                    Language = body.Language,
                    IsForced = body.IsForced,
                    IsHearingImpaired = body.IsHearingImpaired,
                    Stream = stream
                }).ConfigureAwait(false);
            _providerManager.QueueRefresh(video.Id, new MetadataRefreshOptions(new DirectoryService(_fileSystem)), RefreshPriority.High);

            return NoContent();
        }
    }

    /// <summary>
    /// Encodes a subtitle in the specified format.
    /// </summary>
    /// <param name="id">The media id.</param>
    /// <param name="mediaSourceId">The source media id.</param>
    /// <param name="index">The subtitle index.</param>
    /// <param name="format">The format to convert to.</param>
    /// <param name="startPositionTicks">The start position in ticks.</param>
    /// <param name="endPositionTicks">The end position in ticks.</param>
    /// <param name="copyTimestamps">Whether to copy the timestamps.</param>
    /// <returns>A <see cref="Task{Stream}"/> with the new subtitle file.</returns>
    private Task<Stream> EncodeSubtitles(
        Guid id,
        string? mediaSourceId,
        int index,
        string format,
        long startPositionTicks,
        long? endPositionTicks,
        bool copyTimestamps)
    {
        var item = _libraryManager.GetItemById(id);

        return _subtitleEncoder.GetSubtitles(
            item,
            mediaSourceId,
            index,
            format,
            startPositionTicks,
            endPositionTicks ?? 0,
            copyTimestamps,
            CancellationToken.None);
    }

    /// <summary>
    /// Gets a list of available fallback font files.
    /// </summary>
    /// <response code="200">Information retrieved.</response>
    /// <returns>An array of <see cref="FontFile"/> with the available font files.</returns>
    [HttpGet("FallbackFont/Fonts")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IEnumerable<FontFile> GetFallbackFontList()
    {
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
        var fallbackFontPath = encodingOptions.FallbackFontPath;

        if (!string.IsNullOrEmpty(fallbackFontPath))
        {
            var files = _fileSystem.GetFiles(fallbackFontPath, new[] { ".woff", ".woff2", ".ttf", ".otf" }, false, false);
            var fontFiles = files
                .Select(i => new FontFile
                {
                    Name = i.Name,
                    Size = i.Length,
                    DateCreated = _fileSystem.GetCreationTimeUtc(i),
                    DateModified = _fileSystem.GetLastWriteTimeUtc(i)
                })
                .OrderBy(i => i.Size)
                .ThenBy(i => i.Name)
                .ThenByDescending(i => i.DateModified)
                .ThenByDescending(i => i.DateCreated);
            // max total size 20M
            const int MaxSize = 20971520;
            var sizeCounter = 0L;
            foreach (var fontFile in fontFiles)
            {
                sizeCounter += fontFile.Size;
                if (sizeCounter >= MaxSize)
                {
                    _logger.LogWarning("Some fonts will not be sent due to size limitations");
                    yield break;
                }

                yield return fontFile;
            }
        }
        else
        {
            _logger.LogWarning("The path of fallback font folder has not been set");
            encodingOptions.EnableFallbackFont = false;
        }
    }

    /// <summary>
    /// Gets a fallback font file.
    /// </summary>
    /// <param name="name">The name of the fallback font file to get.</param>
    /// <response code="200">Fallback font file retrieved.</response>
    /// <returns>The fallback font file.</returns>
    [HttpGet("FallbackFont/Fonts/{name}")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesFile("font/*")]
    public ActionResult GetFallbackFont([FromRoute, Required] string name)
    {
        var encodingOptions = _serverConfigurationManager.GetEncodingOptions();
        var fallbackFontPath = encodingOptions.FallbackFontPath;

        if (!string.IsNullOrEmpty(fallbackFontPath))
        {
            var fontFile = _fileSystem.GetFiles(fallbackFontPath)
                .First(i => string.Equals(i.Name, name, StringComparison.OrdinalIgnoreCase));
            var fileSize = fontFile?.Length;

            if (fontFile is not null && fileSize is not null && fileSize > 0)
            {
                _logger.LogDebug("Fallback font size is {FileSize} Bytes", fileSize);
                return PhysicalFile(fontFile.FullName, MimeTypes.GetMimeType(fontFile.FullName));
            }

            _logger.LogWarning("The selected font is null or empty");
        }
        else
        {
            _logger.LogWarning("The path of fallback font folder has not been set");
            encodingOptions.EnableFallbackFont = false;
        }

        // returning HTTP 204 will break the SubtitlesOctopus
        return Ok();
    }
}
