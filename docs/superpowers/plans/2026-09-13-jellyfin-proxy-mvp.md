# jellyfin-proxy MVP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship an independent jellyfin-proxy service that reverse-proxies a remote Jellyfin server, manually pre-downloads originals (+ external text subtitles), serves cache hits as Direct Play (`Static=true`), and forwards playstate to the remote.

**Architecture:** Single Go (or Node) process: HTTP reverse proxy by default, local media cache with Range support, download worker, PlaybackInfo rewriter on hit, WebSocket pass-through. Lives in its **own repository**; this Jellyfin Server tree is protocol reference only.

**Tech Stack:** Go 1.22+ (recommended) **or** Node 20+ / TypeScript; SQLite for cache index + download queue; filesystem blob store; reverse proxy via `net/http/httputil` (Go) or `http-proxy` (Node).

**Spec:** [docs/superpowers/specs/2026-09-13-jellyfin-proxy-design.md](../specs/2026-09-13-jellyfin-proxy-design.md)

## Global Constraints

- Do **not** modify Jellyfin Server core / LibraryManager for MVP.
- Always use remote `ItemId` / `MediaSourceId`; cache key = `(ServerId, ItemId, MediaSourceId)`.
- Prefer caching originals only (`Static=true` / File / Download); never cache HLS segments in v1.
- On cache hit, prefer Direct Play; pass through remote decision on miss.
- Clients under test: Official Web, Official Android, Official iOS/iPad.
- Config via env/file: `REMOTE_BASE_URL`, listen addr, cache dir, quota.

---

## File map (new repository `jellyfin-proxy`)

```text
cmd/jellyfin-proxy/main.go          # process entry
internal/config/config.go           # env/file config
internal/proxy/reverse.go           # default reverse proxy + WebSocket
internal/proxy/routes.go            # route classification (pass / intercept)
internal/playback/rewrite.go        # PlaybackInfo JSON rewriter
internal/cache/store.go             # blob paths + metadata
internal/cache/index.go             # SQLite index
internal/cache/serve.go             # Range static responses
internal/download/queue.go          # job queue
internal/download/worker.go         # fetch File/Download/Static
internal/download/subtitles.go      # external text subs
internal/playstate/forward.go       # Playing* forward + offline queue
internal/admin/api.go               # manual enqueue / list / delete cache
internal/admin/ui/                  # minimal static admin (optional MVP+)
tests/...                           # unit + integration fixtures
README.md                           # runbook
```

---

### Task 1: Scaffold repository and config

**Files:**
- Create: `cmd/jellyfin-proxy/main.go`
- Create: `internal/config/config.go`
- Create: `go.mod` / `README.md`
- Create: `internal/config/config_test.go`

- [ ] **Step 1: Write failing config test**

```go
func TestLoadConfigRequiresRemoteBaseURL(t *testing.T) {
    t.Setenv("REMOTE_BASE_URL", "")
    _, err := config.Load()
    if err == nil {
        t.Fatal("expected error when REMOTE_BASE_URL missing")
    }
}
```

- [ ] **Step 2: Run test, confirm fail**

Run: `go test ./internal/config/ -count=1`  
Expected: FAIL (package/config missing)

- [ ] **Step 3: Implement minimal config + main that loads it and exits 0 on `-h`**

Required fields: `RemoteBaseURL`, `ListenAddr` (default `:8097`), `CacheDir`, `CacheQuotaBytes`, `AdminToken`.

- [ ] **Step 4: Re-run test**

Run: `go test ./internal/config/ -count=1`  
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add go.mod cmd internal/config README.md
git commit -m "chore: scaffold jellyfin-proxy config and entrypoint"
```

---

### Task 2: Reverse proxy pass-through (HTTP + WebSocket)

**Files:**
- Create: `internal/proxy/reverse.go`
- Create: `internal/proxy/routes.go`
- Create: `internal/proxy/reverse_test.go`

- [ ] **Step 1: Write failing test that unclassified `/Items` is marked pass-through**

```go
func TestClassifyItemsPassThrough(t *testing.T) {
    if got := Classify(http.MethodGet, "/Items"); got != ActionPassThrough {
        t.Fatalf("got %v", got)
    }
}
```

- [ ] **Step 2: Run test, confirm fail**

Run: `go test ./internal/proxy/ -count=1`  
Expected: FAIL

- [ ] **Step 3: Implement classifier + `httputil.ReverseProxy` to `REMOTE_BASE_URL`; upgrade WebSocket**

Preserve `Authorization`, query `ApiKey`/`api_key`, and hop-by-hop header rules.

- [ ] **Step 4: Manual smoke**

Run proxy; point curl at `/System/Info/Public` via proxy; expect remote JSON.

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: reverse-proxy pass-through for HTTP and WebSocket"
```

---

### Task 3: Cache index and Range file server

**Files:**
- Create: `internal/cache/store.go`
- Create: `internal/cache/index.go`
- Create: `internal/cache/serve.go`
- Create: `internal/cache/serve_test.go`

- [ ] **Step 1: Write failing Range test**

Serve a temp file; request `Range: bytes=0-3`; expect `206` and four bytes.

- [ ] **Step 2: Run test, confirm fail**

Run: `go test ./internal/cache/ -count=1`  
Expected: FAIL

- [ ] **Step 3: Implement SQLite index + filesystem layout**

```text
{CacheDir}/blobs/{serverId}/{itemId}/{mediaSourceId}/media.bin
{CacheDir}/blobs/.../sub_{index}.{ext}
{CacheDir}/index.sqlite
```

- [ ] **Step 4: Re-run tests**

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: media cache index and Range static serving"
```

---

### Task 4: Download worker (manual enqueue)

**Files:**
- Create: `internal/download/queue.go`
- Create: `internal/download/worker.go`
- Create: `internal/download/subtitles.go`
- Create: `internal/admin/api.go`
- Create: `internal/download/worker_test.go`

- [ ] **Step 1: Write failing test with httptest serving a fake media body; worker stores blob and index row**

- [ ] **Step 2: Run test, confirm fail**

- [ ] **Step 3: Implement enqueue API**

`POST /_proxy/v1/downloads` body: `{ "itemId", "mediaSourceId?" }`  
Auth: `AdminToken`.  
Fetch order: try `/Items/{id}/File` → `/Items/{id}/Download` → `/Videos/{id}/stream?Static=true`.  
Also list external subtitle streams from item details and download Stream URLs.

- [ ] **Step 4: Add list/delete admin endpoints**

`GET /_proxy/v1/downloads`, `DELETE /_proxy/v1/cache/{itemId}`

- [ ] **Step 5: Commit**

```bash
git commit -m "feat: manual download queue and admin API"
```

---

### Task 5: Intercept Static streams on cache hit

**Files:**
- Modify: `internal/proxy/routes.go`
- Modify: `internal/proxy/reverse.go`
- Create: `internal/proxy/media.go`
- Create: `internal/proxy/media_test.go`

- [ ] **Step 1: Write failing test**

Classifier: `GET /Videos/{id}/stream.mp4?Static=true` → `ActionCacheMedia`.

- [ ] **Step 2: On hit, serve local Range response with jellyfin-like headers; on miss, reverse-proxy**

Match real client pattern:

`/Videos/{id}/stream.mp4?Static=true&mediaSourceId=...&Tag=...&ApiKey=...`

- [ ] **Step 3: Integration test with seeded cache row**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: serve cached Static streams from local disk"
```

---

### Task 6: PlaybackInfo rewriter

**Files:**
- Create: `internal/playback/rewrite.go`
- Create: `internal/playback/rewrite_test.go`
- Modify: `internal/proxy/routes.go`

- [ ] **Step 1: Write failing test with fixture PlaybackInfo JSON**

On hit: `SupportsDirectPlay=true`, `TranscodingUrl` empty/absent, default MediaSource points at Static stream path.

- [ ] **Step 2: Implement rewrite; miss → unmodified bytes**

For Web + HEVC unknown support: still advertise Direct Play if file is H.264/AAC/MP4; if codec profile is HEVC/HDR, leave remote decision (document in README).

- [ ] **Step 3: External subtitle DeliveryMethod stay External; rewrite DeliveryUrl host only if absolute**

- [ ] **Step 4: Commit**

```bash
git commit -m "feat: rewrite PlaybackInfo toward Direct Play on cache hit"
```

---

### Task 7: Playstate forwarder + offline queue

**Files:**
- Create: `internal/playstate/forward.go`
- Create: `internal/playstate/queue.go`
- Create: `internal/playstate/forward_test.go`

- [ ] **Step 1: Write failing test**

When upstream returns 502, progress is queued; `Flush` replays successfully against httptest.

- [ ] **Step 2: Intercept `/Sessions/Playing`, `/Sessions/Playing/Progress`, `/Sessions/Playing/Stopped` — always attempt forward; queue on transport errors only**

- [ ] **Step 3: Commit**

```bash
git commit -m "feat: forward playstate with offline retry queue"
```

---

### Task 8: Compat matrix verification harness

**Files:**
- Create: `docs/compat-matrix.md` (in jellyfin-proxy repo)
- Create: `scripts/smoke_playback.sh`
- Create: fixtures under `testdata/playbackinfo/`

- [ ] **Step 1: Document verification checklist** (see matrix below)

- [ ] **Step 2: Capture HAR/curl for Web Direct Play Static URL and assert proxy serves 206 from cache**

- [ ] **Step 3: Manual Android + iOS/iPad checklist executed against edge proxy**

- [ ] **Step 4: Commit**

```bash
git commit -m "docs: client compatibility matrix and smoke scripts"
```

---

## Client compatibility matrix & Direct Play rewrite policy

| Client | MVP verify | On cache hit | On miss | Subtitles |
| --- | --- | --- | --- | --- |
| Official Web | Yes | Force Direct Play for H.264/AAC (and remuxed MP4/MKV browser can play); if HEVC/HDR, **do not** force — pass through | Pass through | External VTT/SRT from cache |
| Official Android | Yes | Force Direct Play whenever original cached | Pass through | External text; PGS optional skip |
| Official iOS / iPad | Yes | Force Direct Play on cache hit (reduces relevance of “native player” + “prefer fMP4 HLS” toggles) | Pass through (native+fMP4 only matter here) | External text; graphical → no burn-in in v1 |
| Other | No | Same defaults | Pass through | — |

### Rewrite rules (normative)

1. Cache hit + text-friendly codec set → `SupportsDirectPlay=true`, `SupportsTranscoding=false` optional (prefer soft: leave transcoding true but put Direct Play first / omit TranscodingUrl).
2. Prefer **soft force**: set Direct Play support and Static URLs; only hard-disable transcoding after App matrix proves stable.
3. Cache miss → zero rewrite.
4. Never rewrite Live TV / SyncPlay payloads beyond host pass-through.

### Observed Web Direct Play signature (reference)

```text
/Videos/{itemId}/stream.mp4?Static=true&mediaSourceId=...&deviceId=...&ApiKey=...&Tag=...
```

Proxy must treat this as cacheable Direct Play.

---

### Task 9: README runbook and release checklist

**Files:**
- Modify: `README.md`
- Create: `docker-compose.yml` (optional)

- [ ] **Step 1: Document** remote URL config, cache dir, admin enqueue, client Server URL = proxy, quota, TLS notes

- [ ] **Step 2: Explicit non-goals** (no HLS cache, no Live TV, no core Jellyfin fork)

- [ ] **Step 3: Commit + tag `v0.1.0-mvp` when Tasks 1–8 green**

---

## Out of scope deferrals

- Auto-download on library NewItem WebSocket events (post-MVP)
- Edge local ffmpeg HLS fallback for Web HEVC
- Multi-user credential vault UI
- Plugin inside Jellyfin Server

---

## Execution note

Implement in a **new git repository** named `jellyfin-proxy`. Keep this Jellyfin Server repo limited to protocol reference docs under `docs/superpowers/` unless maintainers explicitly want a link from the main README (not required for MVP).
