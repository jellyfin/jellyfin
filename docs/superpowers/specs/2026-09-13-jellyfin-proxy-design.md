# jellyfin-proxy Design Spec

**Date:** 2026-09-13  
**Status:** Approved for independent-repo MVP planning  
**Context:** Feasibility assessment against Jellyfin Server ~13.x protocol (this repository as reference)

## 1. Product form (confirmed)

**Decision: build an independent transparent reverse-proxy / edge cache.**

| Option | Decision | Reason |
| --- | --- | --- |
| Independent transparent proxy | **Chosen** | Clear boundary; uses remote ItemIds; no LibraryManager conflict |
| Fork / modify Jellyfin Server core as mirror node | **Rejected** | ItemId, library scan, MediaSource, and transcode sessions bind to local FS |
| In-process Jellyfin plugin only | **Not for MVP** | May later host download admin APIs; must not own media serving |

Clients point their Jellyfin Server URL at the proxy. The proxy holds a remote account token, pass-throughs metadata/session APIs, caches original media bytes, and forwards playstate to the remote server.

## 2. Goals and non-goals

### Goals

- Edge deployment on the same LAN as Web / phone / iPad clients
- Manual (and later rule-based) pre-download of originals (+ external text subtitles)
- Cache hit → serve `Static=true` Direct Play from local disk
- Playback progress / played / favorites sync to remote via existing APIs
- Optional rewrite of `PlaybackInfo` on cache hit to prefer Direct Play

### Non-goals (v1)

- Live TV caching
- Caching transcoded / HLS segment output
- SyncPlay enhancements
- Multi-remote backends
- Rebuilding a local Jellyfin library / rescan

## 3. Architecture

```text
Client (Web / Android / iOS|iPad)
    │  Jellyfin HTTP + WebSocket
    ▼
jellyfin-proxy (edge)
    ├─ API reverse proxy (default pass-through)
    ├─ PlaybackInfo rewriter (cache hit → Direct Play)
    ├─ Media cache (Range file server)
    ├─ Download queue (File / Download / Static stream)
    └─ Playstate forwarder (+ offline queue)
    │
    ▼
Remote Jellyfin Server
```

Cache key: `(RemoteServerId, ItemId, MediaSourceId)` plus optional content `Tag`/`ETag`.  
Never mint new ItemIds on the proxy.

## 4. Request routing

### Pass-through

- Browse/search/metadata: `/Items`, `/Users/.../Items`, persons, genres, …
- Auth, sessions, config, `/System/Info*`
- Playstate and UserData writes (proxy may queue if remote down)
- Live TV, SyncPlay, WebSocket, time sync
- Server transcoding / HLS (`master.m3u8`, HLS segments, progressive without `Static=true`)

### Intercept / cache

- `/Videos|Audio/{id}/stream` with `Static=true`
- Pre-download via `/Items/{id}/File` or `/Items/{id}/Download`
- Item images (by tag), external subtitle `.../Subtitles/.../Stream.*`
- Optional trickplay tiles

### JSON rewrite

- `POST/GET /Items/{id}/PlaybackInfo` on cache hit: set `SupportsDirectPlay=true`, clear/avoid forcing `TranscodingUrl`, keep stream URLs relative to proxy
- Auth responses / absolute Live URLs: rewrite host to proxy when required

## 5. Direct Play rewrite strategy

On **cache hit** (full original present):

1. Prefer forcing Direct Play for App clients (Android / iOS native|VLC paths that accept the codec).
2. Keep stream URL shape clients already use, e.g.  
   `/Videos/{id}/stream.mp4?Static=true&mediaSourceId=...&Tag=...`
3. External **text** subtitles: `DeliveryMethod=External`, serve from local cache.
4. Graphical subs (PGS/VobSub): do not force burn-in in v1; play without those tracks or allow optional “compat/HLS fallback” later.
5. On **cache miss**: pass through remote `PlaybackInfo` unchanged (including HLS).

Do **not** promise universal forced Direct Play for Web + HEVC; Web may still need HLS fallback when the browser cannot decode.

## 6. Subtitles

- Pre-download external text tracks with the video.
- On hit, answer subtitle Stream requests from disk (convert to VTT/SRT if needed).
- Never select `Encode` (burn-in) while advertising forced Direct Play.

## 7. Progress sync

Forward (Jellyfin Server routes in this tree):

- `POST /Sessions/Playing`
- `POST /Sessions/Playing/Progress`
- `POST /Sessions/Playing/Stopped`
- optionally `POST /Sessions/Playing/Ping` when a transcoder session exists on miss/fallback

Also forward played/unplayed, favorites, ratings when the client issues them.  
If remote is briefly unreachable: local pending queue, replay on reconnect.

## 8. Client compatibility matrix (selected)

| Client | Priority | Direct Play expectation | Notes |
| --- | --- | --- | --- |
| Official Web | P0 | H.264/AAC/MP4 strong; HEVC weak | Validate Static Direct Play URL pattern seen in the wild |
| Official Android app | P0 | Broad Direct Play | Primary phone target |
| Official iOS / iPad (Swiftfin or Jellyfin iOS) | P0 | Depends on native vs VLC; native + fMP4 matters only for HLS path | When proxy forces Direct Play, native/fMP4 HLS toggles matter less |
| jellyfin-mpv-shim / other | P2 | Out of MVP | |

**MVP verification set:** Web + Android + one iPad/iOS build.

## 9. Security & ops

- TLS on the LAN if credentials cross untrusted links
- Proxy stores remote token + plaintext media → filesystem permissions / optional at-rest encryption
- Disk quota + LRU for cache
- Remote user needs download or static-stream access (`EnableContentDownloading` or equivalent stream rights)

## 10. Success criteria

1. Client uses only the proxy Server URL.
2. Manually cached title plays via `Static=true` from edge disk (LAN).
3. Progress appears on the remote server and other devices.
4. Uncached titles still play via pass-through (possibly remote HLS).
5. No changes required inside Jellyfin Server core for MVP.
