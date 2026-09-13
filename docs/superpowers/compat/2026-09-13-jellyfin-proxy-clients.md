# jellyfin-proxy client compatibility matrix

**Date:** 2026-09-13  
**Related:** [design spec](../specs/2026-09-13-jellyfin-proxy-design.md), [MVP plan](../plans/2026-09-13-jellyfin-proxy-mvp.md)

## Selected verification clients

| Priority | Client | Why |
| --- | --- | --- |
| P0 | Official Jellyfin Web | Dominant LAN browser usage; known `stream.mp4?Static=true` Direct Play shape |
| P0 | Official Android app | Primary phone client; strong Direct Play |
| P0 | Official iOS / iPad app | Edge LAN tablets/phones; native vs VLC + fMP4 HLS settings |
| P2 | Third-party (mpv-shim, etc.) | Deferred |

## Direct Play rewrite strategy (proxy)

| Situation | Policy |
| --- | --- |
| Cache hit, H.264/AAC (Web-safe) | Prefer Direct Play; answer Static stream from disk |
| Cache hit, HEVC/HDR on Web | Do **not** hard-force Direct Play; pass through remote decision (may HLS) |
| Cache hit, Android / iOS app | Prefer Direct Play for cached original |
| Cache miss | No PlaybackInfo rewrite |
| External text subs | Cache + External delivery; keep video Direct Play |
| PGS / VobSub | v1: no burn-in; omit or play without those tracks |

### Soft vs hard force

- **MVP default = soft force:** advertise Direct Play + Static URLs; do not strip transcoding capability until App tests pass.
- **Hard force** (`SupportsTranscoding=false`) only after Web/Android/iOS checklist is green for target libraries.

## iOS settings interaction

- **Use native video player** / **Prefer fMP4 in HLS** matter when the session is **HLS**.
- If proxy cache hit successfully Direct Plays, those toggles have little effect.
- On miss/fallback HLS, native+fMP4 remain user-controlled on device.

## Manual test checklist

### Web

- [ ] Cached title network panel shows `Static=true` (not `master.m3u8`)
- [ ] Seek / Range works; resume position syncs on remote
- [ ] External SRT/VTT displays
- [ ] Uncached title still plays (pass-through)

### Android

- [ ] Cached title Direct Plays from proxy host
- [ ] Progress visible on Web after stop
- [ ] Uncached title plays

### iOS / iPad

- [ ] Cached title plays with Direct Play (verify no HLS playlist fetch)
- [ ] Repeat with native player on/off — cached path should still work
- [ ] Uncached HLS path still works (document native/fMP4 if needed)

## Exit criteria

All P0 checklists pass against an edge-deployed jellyfin-proxy with at least one manually cached movie/episode.
