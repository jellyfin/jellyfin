# SyncPlay File Index

This index maps every SyncPlay-related area in the repository to its role.

## Client Runtime (`Client/jellyfin-web-master/src`)

### Toolbar + React hooks

- `apps/experimental/components/AppToolbar/SyncPlayButton.tsx`
  - Toolbar entrypoint and visibility/access gating.
- `apps/experimental/components/AppToolbar/menus/SyncPlayMenu.tsx`
  - Group create/join/leave/start-stop/settings actions.
- `hooks/useSyncPlayGroups.ts`
  - React-query polling hook for `/SyncPlay/V2/List`.

### Plugin bootstrap

- `plugins/syncPlay/plugin.ts`
  - Registers wrappers and initializes SyncPlay manager.
- `plugins/syncPlay/core/index.js`
  - Exports shared runtime instances.

### Core orchestration

- `plugins/syncPlay/core/Manager.js`
  - Top-level coordinator, v2 reconcile loop, snapshot/command application.
- `plugins/syncPlay/core/Controller.js`
  - User-driven sync actions and command throttling/gates.
- `plugins/syncPlay/core/PlaybackCore.js`
  - Local command scheduling and sync correction mechanics.
- `plugins/syncPlay/core/QueueCore.js`
  - Queue update handling and playback bootstrap.
- `plugins/syncPlay/core/Helper.js`
  - Playback item/event helper utilities.
- `plugins/syncPlay/core/Settings.js`
  - SyncPlay namespaced app settings.
- `plugins/syncPlay/core/V2Api.js`
  - Protocol write helpers for `/SyncPlay/V2/*`.

### Time sync

- `plugins/syncPlay/core/timeSync/TimeSync.js`
  - Generic offset/ping estimation engine.
- `plugins/syncPlay/core/timeSync/TimeSyncCore.js`
  - Time sync facade used by manager/playback.
- `plugins/syncPlay/core/timeSync/TimeSyncServer.js`
  - Server-time ping transport implementation.

### Player abstractions

- `plugins/syncPlay/core/players/PlayerFactory.js`
  - Wrapper resolution for active player type.
- `plugins/syncPlay/core/players/GenericPlayer.js`
  - Base wrapper contract for sync control.
- `plugins/syncPlay/ui/players/NoActivePlayer.js`
  - Fallback wrapper when no local player is bound.
- `plugins/syncPlay/ui/players/HtmlVideoPlayer.js`
  - Video-specific wrapper behavior/events.
- `plugins/syncPlay/ui/players/HtmlAudioPlayer.js`
  - Audio-specific wrapper behavior/events.
- `plugins/syncPlay/ui/players/QueueManager.js`
  - Local queue interaction helpers for wrappers.

### Legacy/group sheet + settings UI

- `plugins/syncPlay/ui/groupSelectionMenu.js`
  - Legacy-style group selection action sheet.
- `plugins/syncPlay/ui/groupSelectionMenu.scss`
  - Styling for the legacy group menu.
- `plugins/syncPlay/ui/playbackPermissionManager.js`
  - Permission checks for syncplay playback actions.
- `plugins/syncPlay/ui/settings/SettingsEditor.js`
  - SyncPlay settings editor logic and profiles.
- `plugins/syncPlay/ui/settings/editor.html`
  - Settings editor template.

## Server API + Orchestration

### API boundary (`Jellyfin.Api`)

- `Jellyfin.Api/Controllers/SyncPlayController.cs`
  - All SyncPlay HTTP endpoints and request mapping.
- `Jellyfin.Api/Auth/SyncPlayAccessPolicy/SyncPlayAccessRequirement.cs`
- `Jellyfin.Api/Auth/SyncPlayAccessPolicy/SyncPlayAccessHandler.cs`
  - Access policy requirements and enforcement.
- `Jellyfin.Api/Models/SyncPlayDtos/*.cs`
  - API request DTO contracts for queue/transport/group actions.

### Group/session orchestrator (`Emby.Server.Implementations`)

- `Emby.Server.Implementations/SyncPlay/SyncPlayManager.cs`
  - Group registry, session membership mapping, request dispatch.
- `Emby.Server.Implementations/SyncPlay/Group.cs`
  - Per-group state machine context, revisioned snapshots, participant state.

### Domain contracts and state machine (`MediaBrowser.Controller`)

- `MediaBrowser.Controller/SyncPlay/ISyncPlayManager.cs`
- `MediaBrowser.Controller/SyncPlay/IGroupStateContext.cs`
- `MediaBrowser.Controller/SyncPlay/IGroupState.cs`
- `MediaBrowser.Controller/SyncPlay/ISyncPlayRequest.cs`
- `MediaBrowser.Controller/SyncPlay/IGroupPlaybackRequest.cs`
  - Core SyncPlay interfaces.
- `MediaBrowser.Controller/SyncPlay/GroupMember.cs`
  - Per-session group member tracking fields.
- `MediaBrowser.Controller/SyncPlay/Queue/PlayQueueManager.cs`
  - Queue mutation and current-item positioning.
- `MediaBrowser.Controller/SyncPlay/GroupStates/*.cs`
  - Idle/Waiting/Playing/Paused behavior implementations.
- `MediaBrowser.Controller/SyncPlay/PlaybackRequests/*.cs`
  - Typed action handlers (Pause/Seek/Queue/Ready/Ping/etc.).
- `MediaBrowser.Controller/SyncPlay/Requests/*.cs`
  - Group-level request contracts (New/Join/Leave/List).
- `MediaBrowser.Controller/Net/WebSocketMessages/Outbound/SyncPlayCommandMessage.cs`
  - WebSocket envelope for SyncPlay command updates.

### Shared model contracts (`MediaBrowser.Model`)

- `MediaBrowser.Model/SyncPlay/*.cs`
  - Shared SyncPlay DTOs/events/enums used by API, server, and clients.
  - Includes v2 snapshot/state contracts (`SyncPlayGroupStateV2Dto`, `SyncPlayGroupSnapshotDto`).

### Data enums

- `src/Jellyfin.Database/Jellyfin.Database.Implementations/Enums/SyncPlayUserAccessType.cs`
- `Jellyfin.Data/Enums/SyncPlayAccessRequirementType.cs`
  - Access-level and policy enum types.

## Tests

- `tests/Jellyfin.Api.Tests/Controllers/SyncPlayControllerTests.cs`
  - Controller request/response coverage.
- `tests/Jellyfin.Server.Implementations.Tests/SyncPlay/GroupV2StateTests.cs`
  - Group snapshot/revision/state behavior coverage.

