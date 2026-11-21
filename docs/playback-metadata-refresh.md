# Isolating playback discovery from descriptive metadata refresh

[中文](playback-metadata-refresh.zh-CN.md)

Status (2026-10-19): implemented and tested on `codex/upstream-playback-metadata`, based directly on official `master` commit `08c2ba6e0b6499122822f1c5b18bc426104538b4`. This is engineering reference material for review, not a submitted PR body. No upstream PR has been created. Only isolated fixtures were used; the existing installed server and real library were not used or changed.

## Workflow and observed problem

We import a library from NFO files, then use an external tool and Jellyfin's editing API to translate and normalize genres and production locations. The database contains the edited values while the source NFO can retain its old values. Subsequently requesting playback information can reimport the old descriptions, despite the user never requesting a descriptive refresh.

Our original library uses symlinks and a custom fast-import mode that defers probing video files. This patch has no dependency on that customization. The reproduction below uses the official STRM path and an HTTP-served generated video, with no plugins or custom import options.

In upstream `MediaSourceManager.GetPlaybackMediaSources`, a non-placeholder source enters `FullRefresh` when probing is allowed and either the item is STRM or its primary audio/video stream is missing. This is the ordinary metadata pipeline: local NFO providers, other descriptive providers and embedded tags can participate. NFO `lockdata`, destination `LockedFields` and merge rules determine the actual result. `ReplaceAllMetadata=false` alone does not prevent this path from modifying descriptions.

## Reproduced before and after

Both builds use the same official base and the same fixture script. Each run creates a new temporary server/database and identical generated media/NFO inputs, with random private credentials and loopback ports. NFO saving, scheduled triggers, realtime monitoring, remote metadata fetchers and image fetchers are disabled. The script waits for initial scanning to finish before editing.

It imports NFO values, submits a complete DTO with API-edited descriptions and **no field locks**, reads it back, then issues two POST PlaybackInfo requests and rereads the metadata after each. Both baseline and patched servers return a usable 160×90 video stream. Source NFO bytes remain unchanged.

| Case | Unmodified official base, after playback | Core patch, after both playback requests |
| --- | --- | --- |
| NFO `lockdata=true`; API genres `剧情`, locations `中国` | Genres become `Original genre`; locations become `Original country, 中国`; title/overview revert to NFO and item lock becomes true | API genres, locations, title, overview and lock state all remain unchanged |
| NFO `lockdata=false`; same API edits | Genres remain edited, but `Original country` is appended to locations | All edited values remain unchanged |
| NFO `lockdata=true`; API explicitly clears genres, locations and overview | Old NFO values refill those empty fields | Empty values remain empty |

The first row is replacement plus collection merging, not a claim that every field is replaced in every NFO configuration. The unlocked NFO case demonstrates a different merge outcome from the same inappropriate playback refresh entry.

Sanitized actual API observations: [before](evidence/playback-before.json), [after](evidence/playback-after.json). The patched run also temporarily makes its generated source return HTTP 404: existing streams, runtime and descriptions remain available. An explicit replace-all refresh after restoring the source still imports the original NFO genre, confirming that ordinary refresh behavior remains available.

## Implementation boundary

1. Playback passes `RefreshMediaInfoOnly=true` and disables image refresh. The current upstream probe trigger is unchanged, including repeated STRM probing.
2. `BaseItem.RefreshMetadata` skips owned-item/image maintenance in this scope. `MetadataService` runs only custom providers implementing `IMediaInfoProvider`; `ProbeProvider` implements that contract. It bypasses NFO/remote descriptions, ordinary before/after refresh hooks, image providers and descriptive refresh timestamps, including first-time and locked items.
3. Video embedded descriptions and people are excluded. Audio tag-based descriptions are excluded. Discovery still handles technical media streams, existing external subtitles/audio, attachments and chapters; playback does not initiate subtitle downloads or chapter-image extraction. A result without the required primary stream is rejected before technical data is replaced.
4. `ItemUpdateType.MediaInfo` selects a scoped item save. `SaveMediaInfoAsync` reads the current database row in a transaction and updates only runtime, bitrate, size, width, height and an explicit allowlist of technical JSON properties. Current descriptions, field locks, refresh timestamps, unrelated JSON properties and version relationships are retained. The normal complete-item save, NFO savers, image saving and version reconstruction are bypassed; the item cache is invalidated after saving.
5. `Video.UpdateToRepositoryAsync` does not copy descriptions to alternate versions for this update type. Otherwise probing a primary could still overwrite independently edited alternate descriptions, even with the NFO provider excluded.
6. Failed/canceled provider execution restores the cached item's technical scalar/array properties and does not perform the scoped item save. A provider reporting no usable update also restores this state.

The necessary persistence and alternate-version boundaries belong with the entry fix. This branch does not include the separate collection merge fix, atomic NFO saver, fast-import/Moonfin changes, translation tool, new reset endpoint, version tokens, or stream-reuse policy.

Existing HTTP request/response shapes and metadata editing remain compatible. Ordinary user-requested refresh/replace-all behavior is unchanged. No database migration is added. Server-side provider/persistence interfaces gain the technical-scope contract; custom implementations must support it, and custom technical providers must not perform descriptive writes in that scope.

### Limits kept explicit

This patch prevents playback discovery from rewriting descriptive metadata. It does not arbitrate concurrent explicit editing requests or stale full DTO submissions. It does not make all media-stream, attachment, chapter and item writes one crash-atomic transaction: the pre-existing technical repositories retain their independent saves. The network/probe-failure and cancellation tests must not be interpreted as proof of rollback after every possible database failure or process crash. Those capabilities remain separate proposals.

It also deliberately retains the existing probe selection rules. Optimizing repeated STRM probes, selecting missing streams per version, and source freshness/revalidation should be reviewed separately. The real HTTP reproduction covers movies; audio, missing-primary-stream decisions and alternate-description boundaries are covered by automated component tests. This is PlaybackInfo validation, not a GUI player/transcoding end-to-end test.

## Reproduction and validation

The fixture accepts only a server DLL and an ffmpeg executable. It cannot target an existing server URL or accept existing credentials. Its libraries, generated media, config, cache and database are all under a newly created temporary directory. It stops only its own child server and retains its report for inspection.

Build the unmodified base into a separate output directory before applying this patch, then build the patch into another. From a checkout containing the fixture, run:

```sh
python3 tests/fixtures/playback_metadata.py \
  --server-dll /absolute/baseline/jellyfin.dll \
  --ffmpeg /absolute/bin/ffmpeg --expect-overwrite

python3 tests/fixtures/playback_metadata.py \
  --server-dll /absolute/patched/jellyfin.dll \
  --ffmpeg /absolute/bin/ffmpeg
```

`--expect-overwrite` asserts the known baseline corruption. Running the normal preservation assertion against the unmodified base was also verified to exit 1 on the locked-NFO case; the patched build exits 0. Without that switch, any change to the curated descriptive/lock values fails the test. The patched run also checks source unavailability and explicit replace-all behavior. No SQLite manipulation is used to manufacture the HTTP failure.

Validation on macOS arm64, .NET SDK 10.0.301, Release configuration:

| Project | Passed | Skipped |
| --- | ---: | ---: |
| Jellyfin.Providers.Tests | 531 | 0 |
| Jellyfin.Controller.Tests | 216 | 0 |
| Jellyfin.Server.Implementations.Tests | 1081 | 13 |
| Jellyfin.Api.Tests | 165 | 0 |
| Jellyfin.XbmcMetadata.Tests | 38 | 0 |
| **Total** | **2031** | **13** |

These totals include existing tests; 24 regression cases were added for this change. The skips are existing platform-specific filesystem cases. Tests use `LANG=en_US.UTF-8 LC_ALL=en_US.UTF-8`: two existing localization expectations require English and initially failed under the host's Chinese locale; no production localization code was changed.

Coverage includes provider exclusion for edited/empty/first-refresh/locked items; embedded video/audio description preservation; failure, cancellation and unusable results; copied refresh scope; unchanged ordinary-file/STRM trigger decisions; skipping owned-item maintenance and alternate-version description copying; scoped persistence retaining newer descriptions/locks/relationships/unrecognized JSON; and bypassing full-item/NFO saves. Existing explicit-refresh tests and the HTTP replace-all check guard ordinary behavior.

The Debug analyzer build also passed using a temporary Microsoft.Net.Compilers.Toolset 5.9.0 compiler. The official analyzer references Roslyn 5.9 while SDK 10.0.301 ships Roslyn 5.6; the matching compiler was used outside the checkout without changing package versions. Analyzer warnings in existing code remain; no analyzer errors remain.

Local builds use an external, uncommitted MSBuild target to inject `AssemblyInformationalVersion=13.0.0-20261019233142`; numeric assembly versions are unchanged. The public server API still reports the official numeric version. This build metadata customization is not part of the patch.

## Review and delivery

The branch targets `jellyfin/jellyfin:master` but is pushed only to the user's fork pending review. The core code, regression fixture and evidence travel together; neither collection merging nor NFO file-save protection is a prerequisite. English is the intended upstream reference, with Chinese maintained for local review. Creating the upstream PR and writing its submission text remain a separate, user-reviewed step.

Two local review passes checked provider/owned-item/version/persistence boundaries, then failure/no-result handling, before/after evidence and bilingual consistency. They identified the alternate-description copy path and restoration after an unusable probe; the Debug check also corrected asynchronous disposal. These are local self-reviews, not upstream maintainer approval.
