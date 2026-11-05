using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Jellyfin.Api.Extensions;
using Jellyfin.Api.Helpers;
using MediaBrowser.Common.Api;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.MediaEncoding;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Net.Http.Headers;

namespace Jellyfin.Api.Controllers;

/// <summary>Owner-bound, bounded, expiring offline MP4 preparation and byte-range transfer.</summary>
[Route("Videos/{itemId:guid}/OfflineDownloads")]
[Authorize]
public class OfflineDownloadsController : BaseJellyfinApiController
{
    private readonly OfflineDownloadJobs _jobs;
    private readonly ITranscodeManager _transcodes;

    /// <summary>Initializes a new instance of the <see cref="OfflineDownloadsController"/> class.</summary>
    /// <param name="configuration">Existing transcode storage configuration.</param>
    /// <param name="lifetime">The server lifetime, not a request lifetime.</param>
    /// <param name="transcodes">Existing encoder process owner.</param>
    /// <param name="logger">Private server diagnostics.</param>
    public OfflineDownloadsController(IServerConfigurationManager configuration, IHostApplicationLifetime lifetime, ITranscodeManager transcodes, ILogger<OfflineDownloadsController> logger)
    {
        _jobs = OfflineDownloadRegistry.Get(configuration, lifetime, reason => logger.LogWarning("Offline cache record rejected during recovery: {Reason}", reason));

        _transcodes = transcodes;
    }

    /// <summary>Creates or reuses a temporary download with identical source and track choices.</summary>
    /// <param name="itemId">The video.</param>
    /// <param name="quality">The bounded preset.</param>
    /// <param name="audioStreamIndex">An available audio index.</param>
    /// <param name="subtitleStreamIndex">An available subtitle index.</param>
    /// <param name="subtitleMode">none, embed, or burn.</param>
    /// <returns>A descriptor without filesystem paths or bearer credentials.</returns>
    [HttpPost]
    [Authorize(Policy = Policies.Download)]
    public ActionResult Create([FromRoute] Guid itemId, [FromQuery] string quality = "720p", [FromQuery] int? audioStreamIndex = null, [FromQuery] int? subtitleStreamIndex = null, [FromQuery] string subtitleMode = "none")
    {
        NoStore();
        var invalid = Videos().ValidateOfflineRequest(itemId, quality, audioStreamIndex, subtitleStreamIndex, subtitleMode, out var item);
        if (invalid is not null)
        {
            return invalid;
        }

        // Resolve "source default" once so later preference changes cannot silently change cached audio.
        audioStreamIndex ??= item!.GetMediaStreams().Where(stream => stream.Type == MediaBrowser.Model.Entities.MediaStreamType.Audio).OrderByDescending(stream => stream.IsDefault).ThenBy(stream => stream.Index).FirstOrDefault()?.Index;
        try
        {
            return new OkObjectResult(_jobs.Create(User.GetUserId(), itemId, Fingerprint(item!), OfflineDownloadProfiles.CreateFileName(item!.Path, quality), quality, audioStreamIndex, subtitleStreamIndex, subtitleMode));
        }
        catch (InvalidOperationException error)
        {
            return new ObjectResult(error.Message) { StatusCode = StatusCodes.Status429TooManyRequests };
        }
    }

    /// <summary>Lists this user's temporary jobs for an accessible video.</summary>
    /// <param name="itemId">The video.</param>
    /// <returns>Owned, accessible jobs only.</returns>
    [HttpGet]
    [Authorize(Policy = Policies.Download)]
    public ActionResult List([FromRoute] Guid itemId)
    {
        NoStore();
        var invalid = Videos().ValidateOfflineRequest(itemId, "720p", null, null, "none", out var item);
        if (invalid is not null)
        {
            return invalid;
        }

        var fingerprint = Fingerprint(item!);
        var jobs = _jobs.List(User.GetUserId(), itemId).Where(job => _jobs.Get(User.GetUserId(), itemId, job.Id, fingerprint) is not null).ToArray();
        return new OkObjectResult(jobs);
    }

    /// <summary>Gets preparation status after current permissions and source checks.</summary>
    /// <param name="itemId">The video.</param>
    /// <param name="jobId">An owned job.</param>
    /// <returns>The descriptor or an authorization/not-found response.</returns>
    [HttpGet("{jobId:guid}")]
    [Authorize(Policy = Policies.Download)]
    public ActionResult Status([FromRoute] Guid itemId, [FromRoute] Guid jobId)
    {
        NoStore();
        var invalid = ValidateJob(itemId, jobId, out var job);
        return invalid ?? new OkObjectResult(job);
    }

    /// <summary>Prepares the immutable file within an awaited request scope; disconnection pauses transfer, not preparation.</summary>
    /// <param name="itemId">The video.</param>
    /// <param name="jobId">An owned job.</param>
    /// <returns>The final descriptor, or current status if another prepare already owns it.</returns>
    [HttpPost("{jobId:guid}/Prepare")]
    [Authorize(Policy = Policies.Download)]
    public async Task<ActionResult> Prepare([FromRoute] Guid itemId, [FromRoute] Guid jobId)
    {
        NoStore();
        var invalid = ValidateJob(itemId, jobId, out var job);
        if (invalid is not null)
        {
            return invalid;
        }

        await _jobs.Prepare(User.GetUserId(), itemId, jobId, async token =>
        {
            // Revalidate after waiting for the single encoder, not just when joining its queue.
            var videos = Videos();
            var error = videos.ValidateOfflineRequest(itemId, job!.Quality, job.AudioStreamIndex, job.SubtitleStreamIndex, job.SubtitleMode, out var item);
            if (error is not null || _jobs.Get(User.GetUserId(), itemId, jobId, Fingerprint(item!)) is null)
            {
                throw new InvalidOperationException("The source or permission changed before preparation.");
            }

            HttpContext.Items[OfflineDownloadContext.SessionKey] = jobId.ToString("N", CultureInfo.InvariantCulture);
            HttpContext.Items[OfflineDownloadContext.CancellationKey] = token;
            var result = await videos.DownloadTranscoded(itemId, job.Quality, job.AudioStreamIndex, job.SubtitleStreamIndex, job.SubtitleMode).ConfigureAwait(false);
            if (result is not FileStreamResult file)
            {
                throw new InvalidOperationException("The selected source cannot be prepared.");
            }

            return file.FileStream;
        }).ConfigureAwait(false);
        invalid = ValidateJob(itemId, jobId, out job);
        return invalid ?? new OkObjectResult(job);
    }

    /// <summary>Transfers a ready file with an immutable validator and standard byte-range semantics.</summary>
    /// <param name="itemId">The video.</param>
    /// <param name="jobId">An owned ready job.</param>
    /// <returns>MP4 bytes, a range, or a not-ready/error response.</returns>
    [HttpGet("{jobId:guid}/File")]
    [HttpHead("{jobId:guid}/File")]
    [Authorize(Policy = Policies.Download)]
    public ActionResult Download([FromRoute] Guid itemId, [FromRoute] Guid jobId)
    {
        NoStore();
        var invalid = ValidateJob(itemId, jobId, out var job);
        if (invalid is not null)
        {
            return invalid;
        }

        if (job!.Status != "Ready")
        {
            return new ObjectResult("The offline file is not ready.") { StatusCode = StatusCodes.Status409Conflict };
        }

        try
        {
            return new FileStreamResult(_jobs.Open(User.GetUserId(), itemId, jobId, HttpContext.Abort), "video/mp4")
            {
                FileDownloadName = job.Filename,
                EnableRangeProcessing = true,
                EntityTag = new EntityTagHeaderValue(job.ETag!)
            };
        }
        catch (IOException)
        {
            return NotFound();
        }
    }

    /// <summary>Discards only this user's job, even if download permissions were subsequently revoked.</summary>
    /// <param name="itemId">The original item id.</param>
    /// <param name="jobId">An owned temporary job.</param>
    /// <returns>No content, or not found without cross-user disclosure.</returns>
    [HttpDelete("{jobId:guid}")]
    public async Task<ActionResult> Delete([FromRoute] Guid itemId, [FromRoute] Guid jobId)
    {
        NoStore();
        if (!_jobs.Delete(User.GetUserId(), itemId, jobId))
        {
            return NotFound();
        }

        var session = jobId.ToString("N", CultureInfo.InvariantCulture);
        await _transcodes.KillTranscodingJobs("offline-download-" + session, session, _ => true).ConfigureAwait(false);
        return NoContent();
    }

    private ActionResult? ValidateJob(Guid itemId, Guid jobId, out OfflineDownloadJobInfo? job)
    {
        job = _jobs.Get(User.GetUserId(), itemId, jobId);
        if (job is null)
        {
            return NotFound();
        }

        var invalid = Videos().ValidateOfflineRequest(itemId, job.Quality, job.AudioStreamIndex, job.SubtitleStreamIndex, job.SubtitleMode, out var item);
        if (invalid is not null)
        {
            return invalid;
        }

        job = _jobs.Get(User.GetUserId(), itemId, jobId, Fingerprint(item!));
        return job is null ? NotFound() : null;
    }

    private VideosController Videos()
    {
        var videos = ActivatorUtilities.CreateInstance<VideosController>(HttpContext.RequestServices);
        videos.ControllerContext = ControllerContext;
        return videos;
    }

    private void NoStore()
    {
        Response.Headers.CacheControl = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    private static string Fingerprint(BaseItem item)
    {
        var source = new FileInfo(item.Path);
        var text = new StringBuilder().Append(item.Id).Append('|').Append(source.Length).Append('|').Append(source.LastWriteTimeUtc.Ticks);
        foreach (var stream in item.GetMediaStreams().OrderBy(stream => stream.Index))
        {
            text.Append('|').Append(stream.Index).Append(':').Append(stream.Type).Append(':').Append(stream.Codec).Append(':').Append(stream.Language);
            if (stream.IsExternal && !string.IsNullOrEmpty(stream.Path))
            {
                var external = new FileInfo(stream.Path);
                text.Append(':').Append(external.Exists ? external.Length : -1).Append(':').Append(external.LastWriteTimeUtc.Ticks);
            }
        }

        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
    }
}
