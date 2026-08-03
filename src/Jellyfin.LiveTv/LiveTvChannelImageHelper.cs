using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.LiveTv;
using MediaBrowser.Model.Entities;
using Microsoft.Extensions.Logging;

namespace Jellyfin.LiveTv;

/// <summary>
/// Helpers for keeping Live TV channel icons in sync with guide data.
/// </summary>
internal static class LiveTvChannelImageHelper
{
    /// <summary>
    /// Detects and applies channel icon changes for a whole channel list, in parallel.
    /// </summary>
    /// <param name="channels">The channels and their guide or tuner metadata. Must not contain duplicate items.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> used for change detection.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="progress">Reports the fraction of channels processed.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The channels whose icon changed, and those where only the cache validators changed.</returns>
    internal static async Task<(List<BaseItem> IconChanged, List<BaseItem> ValidatorsChanged)> UpdateChannelImagesAsync(
        IReadOnlyCollection<(LiveTvChannel Channel, ChannelInfo Info)> channels,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        IProgress<double> progress,
        CancellationToken cancellationToken)
    {
        if (channels.Count == 0)
        {
            return ([], []);
        }

        // Both lists only hold in-memory mutations: the caller has to persist them. IconChanged needs
        // ItemUpdateType.ImageUpdate so the new remote icon is downloaded, the rest a plain save.
        var iconChanged = new ConcurrentBag<BaseItem>();
        var validatorsChanged = new ConcurrentBag<BaseItem>();

        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Math.Min(Environment.ProcessorCount, 6),
            CancellationToken = cancellationToken
        };

        var numComplete = 0;

        await Parallel.ForEachAsync(
            channels,
            options,
            async (source, ct) =>
            {
                try
                {
                    var result = await UpdateChannelImageIfNeededAsync(
                        source.Channel,
                        source.Info.ImagePath,
                        source.Info.ImageUrl,
                        httpClientFactory,
                        logger,
                        ct).ConfigureAwait(false);

                    switch (result)
                    {
                        case ChannelImageUpdate.ImageChanged:
                            iconChanged.Add(source.Channel);
                            break;
                        case ChannelImageUpdate.ValidatorsOnly:
                            validatorsChanged.Add(source.Channel);
                            break;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error updating icon for channel {Name}", source.Channel.Name);
                }

                // This phase is network bound and can take minutes on large channel lists.
                progress.Report(Interlocked.Increment(ref numComplete) / (double)channels.Count);
            }).ConfigureAwait(false);

        return ([.. iconChanged], [.. validatorsChanged]);
    }

    /// <summary>
    /// Applies the channel icon from guide or tuner metadata when it actually changed.
    /// </summary>
    /// <param name="item">The channel item.</param>
    /// <param name="imagePath">The local image path from the tuner, if any.</param>
    /// <param name="imageUrl">The remote image URL from the guide provider, if any.</param>
    /// <param name="httpClientFactory">The <see cref="IHttpClientFactory"/> used for change detection.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>What the caller has to persist, if anything.</returns>
    // Any result other than None must be persisted by the caller: item is the shared cached instance,
    // so an unsaved mutation makes the next refresh believe nothing changed and leaves the icon stale.
    internal static async Task<ChannelImageUpdate> UpdateChannelImageIfNeededAsync(
        BaseItem item,
        string? imagePath,
        string? imageUrl,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        var newImageSource = !string.IsNullOrWhiteSpace(imagePath)
            ? imagePath
            : imageUrl;

        if (string.IsNullOrWhiteSpace(newImageSource))
        {
            return ChannelImageUpdate.None;
        }

        // Only remote http(s) sources can be probed for changes; a local tuner path is treated as-is.
        var isRemote = string.IsNullOrWhiteSpace(imagePath)
            && (newImageSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || newImageSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase));

        var primary = item.GetImageInfo(ImageType.Primary, 0);

        // Apply unconditionally when the channel has no primary image yet or the source path/URL changed.
        if (primary is null || !string.Equals(primary.Source, newImageSource, StringComparison.Ordinal))
        {
            ApplySource(item, newImageSource, etag: null, lastModified: null);
            return ChannelImageUpdate.ImageChanged;
        }

        // Same local (tuner) path or a non-http source: keep the cached image, nothing to detect.
        if (!isRemote)
        {
            return ChannelImageUpdate.None;
        }

        // Same remote URL: only re-apply (and re-download) when the picon content actually changed.
        var probe = await ProbeRemoteAsync(newImageSource, primary.ETag, primary.SourceLastModified, httpClientFactory, logger, cancellationToken).ConfigureAwait(false);

        if (probe.Changed)
        {
            ApplySource(item, newImageSource, probe.ETag, probe.LastModified);
            return ChannelImageUpdate.ImageChanged;
        }

        // Newly learned validators have to be saved, otherwise every later refresh probes without them
        // and could never detect a change.
        if (probe.LearnedValidators)
        {
            primary.ETag = probe.ETag;
            primary.SourceLastModified = probe.LastModified;
            return ChannelImageUpdate.ValidatorsOnly;
        }

        return ChannelImageUpdate.None;
    }

    private static void ApplySource(BaseItem item, string source, string? etag, DateTime? lastModified)
    {
        item.SetImagePath(ImageType.Primary, source);

        // SetImagePath preserves fields it does not know about, so update the source/validators explicitly.
        var image = item.GetImageInfo(ImageType.Primary, 0);
        if (image is not null)
        {
            image.Source = source;
            image.ETag = etag;
            image.SourceLastModified = lastModified;
        }
    }

    private static async Task<RemoteProbeResult> ProbeRemoteAsync(
        string url,
        string? storedETag,
        DateTime? storedLastModified,
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            if (!string.IsNullOrEmpty(storedETag))
            {
                request.Headers.TryAddWithoutValidation("If-None-Match", storedETag);
            }

            if (storedLastModified.HasValue)
            {
                request.Headers.TryAddWithoutValidation(
                    "If-Modified-Since",
                    storedLastModified.Value.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
            }

            var client = httpClientFactory.CreateClient(NamedClient.Default);
            // ResponseHeadersRead avoids buffering the body: on a 200 we inspect the validators and
            // dispose without downloading the payload (the actual download happens later, only if needed).
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.NotModified)
            {
                return new RemoteProbeResult(false, false, storedETag, storedLastModified);
            }

            if (!response.IsSuccessStatusCode)
            {
                // Can't determine the state; keep the cached icon rather than re-downloading every refresh.
                logger.LogDebug("Channel icon {Url} returned {StatusCode}; keeping cached image", url, response.StatusCode);
                return new RemoteProbeResult(false, false, storedETag, storedLastModified);
            }

            var newETag = response.Headers.ETag?.ToString();
            var newLastModified = response.Content.Headers.LastModified?.UtcDateTime;

            var hadValidators = !string.IsNullOrEmpty(storedETag) || storedLastModified.HasValue;
            var hasValidators = !string.IsNullOrEmpty(newETag) || newLastModified.HasValue;

            if (!hasValidators)
            {
                // The server exposes no cache validators, so we can't tell whether it changed: re-download.
                return new RemoteProbeResult(true, false, null, null);
            }

            if (!hadValidators)
            {
                // First time we record validators for an already-cached icon; assume the cache is current.
                return new RemoteProbeResult(false, true, newETag, newLastModified);
            }

            // Prefer the ETag when both sides have one: it is authoritative, while Last-Modified is often
            // inconsistent across load-balanced origins and would otherwise cause spurious re-downloads.
            bool unchanged;
            if (!string.IsNullOrEmpty(newETag) && !string.IsNullOrEmpty(storedETag))
            {
                unchanged = string.Equals(newETag, storedETag, StringComparison.Ordinal);
            }
            else
            {
                unchanged = newLastModified.HasValue && newLastModified == storedLastModified;
            }

            // Only worth a save when a validator we did not have before appeared. Drift in one that is
            // already stored (typically Last-Modified varying across load-balanced origins while the
            // authoritative ETag is stable) must not trigger one, or every refresh re-saves every channel.
            var learned = (string.IsNullOrEmpty(storedETag) && !string.IsNullOrEmpty(newETag))
                || (!storedLastModified.HasValue && newLastModified.HasValue);

            return new RemoteProbeResult(!unchanged, learned, newETag, newLastModified);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            // Network error, timeout or unsupported method: keep the cached icon.
            logger.LogDebug(ex, "Unable to check channel icon {Url} for changes; keeping cached image", url);
            return new RemoteProbeResult(false, false, storedETag, storedLastModified);
        }
    }

    // LearnedValidators: a cache validator that was not stored before is now available.
    private readonly record struct RemoteProbeResult(bool Changed, bool LearnedValidators, string? ETag, DateTime? LastModified);
}
