using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.PlaybackReporter.Configuration;
using Jellyfin.Plugin.PlaybackReporter.Models;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.PlaybackReporter.Services;

/// <summary>
/// Creates GitHub issues for recorded playback error events.
/// </summary>
public class GitHubReporter
{
    private const string GitHubApiBase = "https://api.github.com";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GitHubReporter> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="GitHubReporter"/> class.
    /// </summary>
    public GitHubReporter(IHttpClientFactory httpClientFactory, ILogger<GitHubReporter> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Creates a GitHub issue for a playback error event and returns the issue URL,
    /// or <c>null</c> if reporting is not configured or the request fails.
    /// </summary>
    public async Task<string?> ReportAsync(PlaybackErrorEvent evt, PluginConfiguration config, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(config.GitHubToken)
            || string.IsNullOrWhiteSpace(config.GitHubOwner)
            || string.IsNullOrWhiteSpace(config.GitHubRepo))
        {
            _logger.LogWarning("GitHub reporting is not configured. Set GitHubToken, GitHubOwner, and GitHubRepo in the plugin configuration.");
            return null;
        }

        var url = string.Create(CultureInfo.InvariantCulture, $"{GitHubApiBase}/repos/{config.GitHubOwner}/{config.GitHubRepo}/issues");

        var title = BuildTitle(evt);
        var body = BuildBody(evt);

        var labels = config.GitHubIssueLabels
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToArray();

        var payload = new
        {
            title,
            body,
            labels,
        };

        var json = JsonSerializer.Serialize(payload);

        var client = _httpClientFactory.CreateClient(nameof(GitHubReporter));
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", config.GitHubToken);
        request.Headers.UserAgent.Add(new ProductInfoHeaderValue("Jellyfin-PlaybackReporter", "1.0"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "GitHub API returned {StatusCode} when creating issue for event {EventId}: {Body}",
                    response.StatusCode,
                    evt.Id,
                    responseBody);
                return null;
            }

            using var doc = JsonDocument.Parse(responseBody);
            if (doc.RootElement.TryGetProperty("html_url", out var htmlUrl))
            {
                var issueUrl = htmlUrl.GetString();
                _logger.LogInformation("Created GitHub issue for event {EventId}: {IssueUrl}", evt.Id, issueUrl);
                return issueUrl;
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while creating GitHub issue for event {EventId}", evt.Id);
            return null;
        }
    }

    private static string BuildTitle(PlaybackErrorEvent evt)
    {
        var itemName = evt.RequestedItemName ?? "Unknown Item";
        var client = evt.ClientName ?? "Unknown Client";
        var ic = CultureInfo.InvariantCulture;

        return evt.EventType switch
        {
            PlaybackEventType.PlaybackFailure =>
                string.Create(ic, $"[Playback Failure] {itemName} failed to play on {client}"),

            PlaybackEventType.FormatMismatch =>
                string.Create(ic, $"[Format Mismatch] {itemName} transcoded on {client} ({evt.SourceContainer}/{evt.SourceVideoCodec} -> {evt.DeliveredContainer}/{evt.DeliveredVideoCodec})"),

            PlaybackEventType.ItemMismatch =>
                string.Create(ic, $"[Item Mismatch] Requested \"{itemName}\" but played \"{evt.ActualItemName}\" on {client}"),

            PlaybackEventType.HdrPlaybackIssue =>
                string.Create(ic, $"[HDR Issue] {evt.SourceVideoRangeType ?? "HDR"} content transcoded on {client} ({itemName})"),

            _ => string.Create(ic, $"[Playback Issue] {itemName} on {client}"),
        };
    }

    private static string BuildBody(PlaybackErrorEvent evt)
    {
        var ic = CultureInfo.InvariantCulture;
        var sb = new StringBuilder();

        sb.AppendLine("## Playback Error Report");
        sb.AppendLine();
        sb.Append(ic, $"**Event Type:** {evt.EventType}  ").AppendLine();
        sb.Append(ic, $"**Timestamp:** {evt.Timestamp:u}  ").AppendLine();
        sb.Append(ic, $"**Event ID:** `{evt.Id}`  ").AppendLine();
        sb.AppendLine();

        // Client section
        sb.AppendLine("### Client");
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.Append(ic, $"| Client | {Escape(evt.ClientName)} |").AppendLine();
        sb.Append(ic, $"| Device | {Escape(evt.DeviceName)} |").AppendLine();
        sb.Append(ic, $"| Device ID | `{Escape(evt.DeviceId)}` |").AppendLine();
        sb.Append(ic, $"| Session ID | `{Escape(evt.SessionId)}` |").AppendLine();
        sb.Append(ic, $"| Play Session ID | `{Escape(evt.PlaySessionId)}` |").AppendLine();
        sb.AppendLine();

        // Media item section
        sb.AppendLine("### Media Item");
        sb.AppendLine("| Field | Value |");
        sb.AppendLine("|-------|-------|");
        sb.Append(ic, $"| Requested Item | {Escape(evt.RequestedItemName)} |").AppendLine();
        sb.Append(ic, $"| Requested Item ID | `{evt.RequestedItemId}` |").AppendLine();
        sb.Append(ic, $"| Requested Source ID | `{Escape(evt.RequestedMediaSourceId)}` |").AppendLine();

        if (evt.EventType == PlaybackEventType.ItemMismatch)
        {
            sb.Append(ic, $"| **Actual Item Played** | **{Escape(evt.ActualItemName)}** |").AppendLine();
            sb.Append(ic, $"| Actual Source ID | `{Escape(evt.ActualMediaSourceId)}` |").AppendLine();
        }

        sb.AppendLine();

        // Format / Codec section
        sb.AppendLine("### Format & Codec");
        sb.AppendLine("| | Container | Video Codec | Audio Codec |");
        sb.AppendLine("|---|---|---|---|");
        sb.Append(ic, $"| **Source (file)** | {Escape(evt.SourceContainer)} | {Escape(evt.SourceVideoCodec)} | {Escape(evt.SourceAudioCodec)} |").AppendLine();
        sb.Append(ic, $"| **Delivered (client)** | {Escape(evt.DeliveredContainer)} | {Escape(evt.DeliveredVideoCodec)} | {Escape(evt.DeliveredAudioCodec)} |").AppendLine();
        sb.AppendLine();

        sb.Append(ic, $"**Play Method:** {evt.PlayMethod?.ToString() ?? "_unknown_"}  ").AppendLine();

        if (evt.TranscodeReasons.HasValue && evt.TranscodeReasons != 0)
        {
            sb.AppendLine();
            sb.AppendLine("**Transcode Reasons:**");
            foreach (var flag in GetTranscodeReasonFlags(evt.TranscodeReasons.Value))
            {
                sb.Append(ic, $"- {flag}").AppendLine();
            }
        }

        sb.AppendLine();

        // HDR section
        if (evt.EventType == PlaybackEventType.HdrPlaybackIssue || evt.HasDolbyVision
            || !string.IsNullOrEmpty(evt.SourceVideoRangeType))
        {
            sb.AppendLine("### HDR / Dynamic Range");
            sb.AppendLine("| Field | Value |");
            sb.AppendLine("|-------|-------|");
            sb.Append(ic, $"| Video Range | {Escape(evt.SourceVideoRange)} |").AppendLine();
            sb.Append(ic, $"| Video Range Type | {Escape(evt.SourceVideoRangeType)} |").AppendLine();
            sb.Append(ic, $"| Dolby Vision | {(evt.HasDolbyVision ? "Yes" : "No")} |").AppendLine();

            if (evt.HasDolbyVision)
            {
                sb.Append(ic, $"| DV Profile | {evt.DolbyVisionProfile?.ToString(ic) ?? "_unknown_"} |").AppendLine();
                sb.Append(ic, $"| DV Fallback | {Escape(evt.DolbyVisionFallback)} |").AppendLine();
            }

            sb.AppendLine();
        }

        // Playback duration
        if (evt.PlayedDurationSeconds.HasValue)
        {
            sb.Append(ic, $"**Played Duration Before Event:** {evt.PlayedDurationSeconds:F1}s  ").AppendLine();
            sb.AppendLine();
        }

        // Notes
        if (evt.Notes.Count > 0)
        {
            sb.AppendLine("### Additional Notes");
            foreach (var note in evt.Notes)
            {
                sb.Append(ic, $"- {note}").AppendLine();
            }

            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("_Reported automatically by [Jellyfin Playback Reporter Plugin](https://github.com/trumblejoe/jellyfin)_");

        return sb.ToString();
    }

    private static string Escape(string? value)
        => string.IsNullOrEmpty(value) ? "_unknown_" : value.Replace("|", "\\|", StringComparison.Ordinal);

    private static IEnumerable<TranscodeReason> GetTranscodeReasonFlags(TranscodeReason reasons)
    {
        foreach (TranscodeReason flag in Enum.GetValues<TranscodeReason>())
        {
            if (reasons.HasFlag(flag))
            {
                yield return flag;
            }
        }
    }
}
