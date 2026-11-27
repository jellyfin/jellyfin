# Issue #3016: Jellyfin-specific Prometheus metrics

This note describes the Prometheus metrics implemented on the `issue-3016-jf-metrics`
branch, their current limitations, and questions for continuing the work. It is a
handoff for development and issue discussion, not a claim that issue #3016 is
complete or that the metrics have been accepted upstream.

Issue: [Improve Prometheus Exporter with JF Metrics](https://github.com/jellyfin/jellyfin/issues/3016)

## Summary

The branch adds Jellyfin-specific metrics on top of the existing `prometheus-net`
integration. When metrics are enabled, Jellyfin exports its normal HTTP metrics and
the new metrics at `/metrics`. The metrics setting defaults to disabled.

Most current-state gauges are initialized at service startup and refreshed every
15 seconds by a hosted service. Playback counters are incremented on the playback
start event. The library media-stream/codec aggregation is more expensive, so it
is refreshed at most every five minutes.

## Metrics implemented

Metric names and help text are defined in
[`JellyfinMetrics.cs`](../Jellyfin.Server.Implementations/Metrics/JellyfinMetrics.cs).

| Metric | Type | Labels | Meaning and caveats |
|---|---|---|---|
| `jellyfin_user_accounts` | Gauge | — | All user accounts, including disabled and hidden accounts. |
| `jellyfin_connected_sessions` | Gauge | `device_type`, `client` | Currently connected client sessions, grouped by client-reported values. This is not a count of all past sessions or all registered devices. |
| `jellyfin_registered_devices` | Gauge | — | Registered client device records, whether or not currently connected. |
| `jellyfin_active_streams` | Gauge | `play_method` | Connected sessions whose play state has a method: `direct_play`, `direct_stream`, `transcode`, or `unknown`. |
| `jellyfin_active_transcodes` | Gauge | `acceleration`, `video_codec`, `audio_codec` | Connected sessions with transcoding information. Acceleration is the configured mode; this does not prove hardware was used. |
| `jellyfin_play_methods_total` | Counter | `play_method`, `transcode_reason` | Playback starts grouped by play method and stream-transcode reason. Reasons are stable snake_case values joined with `\|`; `none` means no reason was reported and `unknown` means details were unavailable. |
| `jellyfin_transcodes_total` | Counter | `acceleration` | Transcoding playback starts by configured acceleration mode. It does not record why that mode was selected or confirm hardware use. |
| `jellyfin_libraries` | Gauge | `type` | Configured virtual folders by collection type. |
| `jellyfin_library_items` | Gauge | `type` | Library item counts by media type, aggregated across libraries. |
| `jellyfin_media_streams` | Gauge | `library`, `library_id`, `kind`, `codec` | Indexed media stream counts grouped by the item's top-level library, stream kind, and codec. |
| `jellyfin_users_with_permission` | Gauge | `permission` | Number of accounts with each permission in the collector's tracked permission list enabled. |
| `jellyfin_users_authenticating_external` | Gauge | `provider` | User count for non-default authentication providers. The label is the provider ID/type name, not a friendly display name. |

Counters accumulate for the lifetime of the Jellyfin process and reset on restart;
Prometheus can retain their history when it scrapes them. Gauge refreshes clear
previous label combinations that no longer appear so that stale combinations are
reported as zero.

## Implementation map

- [`MetricsCollectionService.cs`](../Jellyfin.Server.Implementations/Metrics/MetricsCollectionService.cs)
  subscribes to playback-start events and refreshes the gauge collectors.
- [`SessionMetricsCollector.cs`](../Jellyfin.Server.Implementations/Metrics/SessionMetricsCollector.cs)
  handles connected sessions, devices, stream gauges, and playback counters.
- [`LibraryMetricsCollector.cs`](../Jellyfin.Server.Implementations/Metrics/LibraryMetricsCollector.cs)
  handles virtual-folder, item-count, and database media-stream metrics.
- [`UserMetricsCollector.cs`](../Jellyfin.Server.Implementations/Metrics/UserMetricsCollector.cs)
  handles account and permission counts.
- [`AuthenticationMetricsCollector.cs`](../Jellyfin.Server.Implementations/Metrics/AuthenticationMetricsCollector.cs)
  handles external authentication provider counts.
- [`MetricsSnapshot.cs`](../Jellyfin.Server.Implementations/Metrics/MetricsSnapshot.cs)
  updates gauge label sets and zeros out values no longer present.
- [`MetricLabels.cs`](../Jellyfin.Server.Implementations/Metrics/MetricLabels.cs)
  maps internal enums/reasons to stable Prometheus label values.
- [`Startup.cs`](../Jellyfin.Server/Startup.cs) registers the hosted collector and
  maps the Prometheus endpoint when metrics are enabled.

Tests for collection, label mapping, lifecycle behavior, and exposition are under
[`tests/Jellyfin.Server.Implementations.Tests/Metrics`](../tests/Jellyfin.Server.Implementations.Tests/Metrics).

## Scope and known gaps

The original issue is an idea list rather than a narrowly defined acceptance
checklist. This branch implements several of its initial requests, but does not
implement every item or later suggestion in the discussion:

- Groups and group permissions are not included; those depend on group support.
- It does not track all historical/inactive sessions. Connected sessions and
  registered device records are different concepts.
- It does not report why a hardware acceleration mode was selected or verify
  actual hardware use.
- Library item counts are global by media type, not split by library.
- It does not provide per-user viewing/watched statistics.
- It does not add CPU, memory, or disk-I/O metrics for the host or spawned
  processes. The issue discussion notes that system-level exporters may be a
  better fit for host resource metrics.
- It does not add scheduled-task duration/failure or library-scan metrics.

The database aggregation for media-stream counts reads and groups indexed stream
records. It is deliberately refreshed less often than the other gauges, but its
impact on large libraries should still be measured.

## Follow-up questions for the issue

Before expanding the implementation, agree on the target scope:

1. Which unimplemented requests from the issue are still desired, and which belong
   in separate issues or external exporters?
2. Is “total sessions” intended to mean registered devices, accounts, or a retained
   history of clients? The current implementation only exposes connected sessions
   and registered devices.
3. Should library item counts be split by library, and is the extra label
   cardinality worth the value?
4. Are task/scan duration and failure metrics useful enough to add instrumentation
   to those systems?
5. What privacy and cardinality limits should apply before adding labels for users,
   client-reported names, or other user-specific information?
6. Should acceleration metrics distinguish configured mode from confirmed
   hardware use, and can Jellyfin reliably observe the latter?
7. Should the database aggregation be benchmarked or redesigned for large
   libraries before merging?

## Why the branch stops at this scope

The branch history shows incremental implementation and refinement of the metrics
listed above; it does not record a formal decision that the issue is complete.
The issue remains a broad, open-ended discussion, and the branch does not explain
why every suggestion was excluded. The clearest recorded scope constraint in the
discussion is that host CPU/memory/disk usage can be difficult to attribute to
Jellyfin when it launches external processes, for which system-level exporters
were suggested. Other gaps above should be treated as open scope decisions, not
as rejected requirements.

