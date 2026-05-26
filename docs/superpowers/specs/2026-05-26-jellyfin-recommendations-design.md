# Jellyfin Recommendations — Taste-Profile Engine Design

**Date:** 2026-05-26
**Status:** Draft, pending user review
**Scope:** Replace the broken movie/series recommendation logic with a per-user "taste profile" engine built from local metadata of items the user has watched and favorited.

## 1. Problem statement

The `Recommended` section in Jellyfin is broken. Two root causes:

1. **`MoviesController.GetSimilarTo` (`Jellyfin.Api/Controllers/MoviesController.cs:264-297`) returns random movies.** During the EF Core migration (PR #12798) the `SimilarTo` parameter on `InternalItemsQuery` was removed. The method was never updated and now runs an `InternalItemsQuery` with no similarity criteria — just `Limit`, `IncludeItemTypes`, `IsMovie`. Each "Because you watched X" category emits `itemLimit` random movies.
2. **`SuggestionsController.GetSuggestions` (`Jellyfin.Api/Controllers/SuggestionsController.cs:62`) returns random items.** Its `OrderBy = (ItemSortBy.Random, SortOrder.Descending)` is the entirety of the ranking logic.

Confirmed by user reports in GitHub issues #14088 ("Movie Suggestions not populating on 10.11"), #15342 ("Random sorting in GetSimilarItems breaks recommendation accuracy"), and #16856 (open PR titled "Fix movie recommendations"). The in-flight PR batches the existing logic but does not introduce new similarity signals.

## 2. Goal

Build a per-user **taste profile** from the metadata of items they have watched and favorited (locally — no remote provider dependency), and use that profile to rank candidate items for the `/Movies/Recommendations` and `/Items/Suggestions` endpoints. Cover both `Movie` and `Series` content kinds with separate per-kind profiles.

## 3. Non-goals

- Not changing `GET /Items/{itemId}/Similar` ("More Like This" on detail pages). That path uses `SimilarItemsManager` + `MovieSimilarItemsProvider` and is a per-item similarity concept, separate from per-user recommendations. Fixing the `OrderBy = Random` regression in that provider is tracked separately by issue #15342.
- Not introducing collaborative filtering, machine learning, or any cross-user signal.
- Not adding remote/external similarity providers (TMDB, etc.) as inputs to the profile.
- Not adding user-facing configuration for signal weights in v1. Weights are tunable constants in code.
- Not adding negative signals (low user ratings) in v1.
- Not changing API routes, request parameters, or response DTOs. Existing clients work unchanged.
- Not changing database schema.

## 4. Architecture

Three new units inside `Emby.Server.Implementations/Library/Recommendations/`, with interfaces in `MediaBrowser.Controller/Library/Recommendations/`:

```
MediaBrowser.Controller/Library/Recommendations/
  IRecommendationsService.cs   -- public-facing API used by controllers
  TasteProfile.cs              -- immutable POCO: weighted dicts of genre/tag/person/studio
  RecommendationRequest.cs     -- input: user, kind, parentId, categoryLimit, itemLimit

Emby.Server.Implementations/Library/Recommendations/
  RecommendationsService.cs    -- orchestrator: profile cache, response builder
  TasteProfileBuilder.cs       -- pure function (watched + favorited items) -> TasteProfile
  TasteProfileScorer.cs        -- pure function (profile, candidate, optional seed) -> float
```

**Responsibilities:**

- `RecommendationsService` is the only type controllers see. It owns the cache, fans work out to builder/scorer, and produces DTOs.
- `TasteProfileBuilder` is a pure transformation. No I/O, no DI on managers. Unit-testable with in-memory fixtures.
- `TasteProfileScorer` is a pure function. Unit-testable in isolation.
- `TasteProfile` is an immutable record. One instance per `(user, BaseItemKind, parentId)`.

**Wiring:**

- `RecommendationsService` registered as a singleton in `Emby.Server.Implementations/ApplicationHost.cs` (around line 551, immediately after the existing `ISimilarItemsManager` registration).
- `MoviesController` and `SuggestionsController` get `IRecommendationsService` via constructor injection.
- `RecommendationsService` constructor subscribes to `IUserDataManager.UserDataSaved` for cache invalidation.

**Scope boundary:** No changes to `SimilarItemsManager`, `MovieSimilarItemsProvider`, `SeriesSimilarItemsProvider`, `InternalItemsQuery`, or database schema. The only existing-code touches beyond the two named controllers are: (a) one DI line in `ApplicationHost.cs`, and (b) a small additive extension to `InternalPeopleQuery` and `PeopleRepository` to add an `ItemIds` (plural) filter — justified below in §6 because without it the profile builder and scorer would do N+1 person fetches.

## 5. Data model

### `TasteProfile`

```csharp
public sealed record TasteProfile(
    BaseItemKind Kind,                              // Movie or Series
    DateTime ComputedAt,                            // diagnostic / TTL fallback
    IReadOnlyDictionary<string, float> Genres,      // genre name -> weight
    IReadOnlyDictionary<string, float> Tags,        // tag name -> weight
    IReadOnlyDictionary<Guid, float> People,        // person id -> weight (cast + crew)
    IReadOnlyDictionary<string, float> Studios,     // studio name -> weight
    float TotalSignalMass);                         // sum of all weights, for normalization
```

String-keyed dictionaries use `StringComparer.OrdinalIgnoreCase`. `People` uses `Guid` because `IPeopleRepository` returns stable IDs (name-matching is brittle across spellings and accents). An empty profile (`TotalSignalMass == 0`) is valid and represents the cold-start state.

### `RecommendationRequest`

```csharp
public sealed record RecommendationRequest(
    Guid UserId,
    BaseItemKind Kind,           // Movie or Series
    Guid? ParentId,              // limit to a library/folder; optional
    int CategoryLimit,           // max categories
    int ItemLimit,               // max items per category
    DtoOptions DtoOptions);
```

### Signal weights (constants in `TasteProfileBuilder`)

Per-item signal strength (these can stack on a single item):

| Signal | Weight |
|---|---|
| `Played == true` | 1.0 |
| `IsFavorite == true` | 2.0 |
| `Likes == true` | 1.5 |

A favorited and fully played item contributes `3.0`. (`Likes` is the legacy Emby thumbs-up flag, preserved for compatibility.)

Per-field contribution into the profile (multiplied by signal strength):

| Field | Weight | Notes |
|---|---|---|
| Genre | 3.0 | Strongest taste signal |
| Tag | 1.5 | Narrower, noisier |
| Person | 1.0 each | Capped to top 5 per item by `SortOrder` |
| Studio | 0.5 | Weak but useful |

All weights are `internal const float` in `TasteProfileBuilder` — one file to tune.

## 6. Algorithm: profile build

`TasteProfileBuilder.Build(user, kind, parentId, libraryManager, peopleRepository)`:

1. Run two queries against `ILibraryManager.GetItemList` (since `InternalItemsQuery.IsPlayed` and `IsFavoriteOrLiked` are independent `bool?` filters that AND when both set, not OR):

   ```
   // Query A: most recent 500 played items
   InternalItemsQuery {
     User = user, IncludeItemTypes = [kind], IsPlayed = true,
     ParentId = parentId, Recursive = true,
     OrderBy = [(LastPlayedDate, Descending)], Limit = 500
   }
   // Query B: favorites/liked items (cap 250)
   InternalItemsQuery {
     User = user, IncludeItemTypes = [kind], IsFavoriteOrLiked = true,
     ParentId = parentId, Recursive = true, Limit = 250
   }
   ```

   Union the two by `Id` (favorites that are also played appear once and pick up both signal weights in step 2). Cap of 500+250 prevents unbounded profile cost on huge histories — the most recent watches and standing favorites dominate taste anyway.
2. For each item compute `signalWeight = (Played ? 1.0 : 0) + (IsFavorite ? 2.0 : 0) + (Likes ? 1.5 : 0)`. Skip if zero (defensive — shouldn't happen given the query filter).
3. For each `genre` in `item.Genres`: `Genres[genre] += signalWeight * 3.0`.
4. For each `tag` in `item.Tags`: `Tags[tag] += signalWeight * 1.5`.
5. For each `studio` in `item.Studios`: `Studios[studio] += signalWeight * 0.5`.
6. Batch-fetch people for the unioned item set via `IPeopleRepository.GetPeople(new InternalPeopleQuery { ItemIds = [...] })` — one DB call, not one per item. **This requires adding an `ItemIds` (plural, `IReadOnlyList<Guid>`) field to `InternalPeopleQuery`** (today it only has `ItemId` singular at `MediaBrowser.Controller/Entities/InternalPeopleQuery.cs:31`) and supporting it in `PeopleRepository.GetPeople`. Without this, both profile build (up to 500 items) and per-request scoring (up to ~150 candidates) would do N+1 fetches — unacceptable. The change is purely additive: existing callers using `ItemId` keep working unchanged. Group the returned people by `ItemId`, take the first 5 by `SortOrder` per item, and: `People[person.Id] += signalWeight * 1.0`.
7. Compute `TotalSignalMass` as the sum of every value across all four dictionaries.
8. Return an immutable `TasteProfile`.

## 7. Algorithm: scoring

`TasteProfileScorer.Score(profile, candidate, seedItem?)`:

1. Initialize `score = 0`.
2. For each `genre` in `candidate.Genres`: if `profile.Genres.TryGetValue(genre, out var w)`, `score += w`.
3. Same for `candidate.Tags`, `candidate.Studios`.
4. For each person on the candidate (via `IPeopleRepository.GetPeople`, top 5 by `SortOrder`): if in `profile.People`, `score += w`.
5. If `seedItem` is provided, add overlap bonuses to anchor the category to the seed:
   - `score += 5.0 * |candidate.Genres ∩ seedItem.Genres|`
   - `score += 2.0 * |candidate.Tags ∩ seedItem.Tags|`
   - `score += 1.0 * |candidatePeople ∩ seedPeople|`
   - `score += 0.5 * |candidate.Studios ∩ seedItem.Studios|`
6. Normalize: `score / (profile.TotalSignalMass + 1.0)`. The `+1.0` epsilon avoids divide-by-zero for cold-start; the result is unbounded above but relative ordering is what matters.
7. Return the float.

Scoring is pure and synchronous. `RecommendationsService` is responsible for batch-fetching the people for the entire candidate pool **before** scoring (one `IPeopleRepository.GetPeople` call with `ItemIds = candidateIds`) and passing the resulting `Dictionary<Guid, IReadOnlyList<PersonInfo>>` to each `Score` call. Per-candidate DB lookups during scoring are explicitly forbidden — they would make scoring a hundred candidates an N+1 query disaster.

## 8. Response building

`RecommendationsService.GetRecommendationsAsync(request, cancellationToken)` for `/Movies/Recommendations`:

1. Acquire cached profile for `(userId, kind, parentId)` or compute it.
2. Pick seed items:
   - Recently played: `InternalItemsQuery { IsPlayed = true, OrderBy = LastPlayedDate desc, Limit = 6 }`.
   - Favorited: `InternalItemsQuery { IsFavoriteOrLiked = true, OrderBy = Random, Limit = 10, ExcludeItemIds = recently-played-ids }`.
3. Extract director and actor names from the recently-played set (re-uses existing `GetPeople` + `MaxListOrder = 3` filter).
4. For each seed, fetch a candidate pool:
   ```
   InternalItemsQuery {
     IncludeItemTypes = [kind],
     Genres = seed.Genres, Tags = seed.Tags,     // cheap DB-side narrowing
     ExcludeItemIds = (already-watched ∪ already-emitted-this-response),
     ParentId = request.ParentId, Recursive = true,
     EnableGroupByMetadataKey = true,
     Limit = itemLimit * 4
   }
   ```
5. Score each candidate with `Score(profile, candidate, seed)`. Sort desc. Take `itemLimit`.
6. **Skip the category** if the resulting list has fewer than `itemLimit / 2` items (avoids "Because you watched X — here are 2 unrelated movies").
7. Emit a `RecommendationDto`:
   - `BaselineItemName = seed.Name`
   - `CategoryId = seed.Id`
   - `RecommendationType = SimilarToRecentlyPlayed` (or `SimilarToLikedItem` for favorite seeds)
   - `Items` = the scored top-N as `BaseItemDto[]`
8. For director/actor seeds: fetch `InternalItemsQuery { Person = name, IncludeItemTypes = [kind] }`, rank by `Score(profile, candidate, seedItem: null)`, emit with `RecommendationType = HasDirectorFromRecentlyPlayed` or `HasActorFromRecentlyPlayed`.
9. Order categories by `RecommendationType` (preserves the existing convention at `MoviesController.cs:176`).
10. Stop when `request.CategoryLimit` is reached.

For `/Items/Suggestions`: `RecommendationsService.GetRankedItemsAsync(request, count)` returns a flat ranked list. If the requested `mediaType[]`/`type[]` is recommendable (single kind = Movie or Series), score the full candidate pool against the profile and return top-N. If types are mixed, unrecommendable (e.g. Photo), or the user is cold-start, the caller (controller) falls back to today's random behavior — no regression.

## 9. Caching and invalidation

- Cache: `ConcurrentDictionary<(Guid UserId, BaseItemKind Kind, Guid ParentId), Lazy<Task<TasteProfile>>>` held by `RecommendationsService`. `Guid.Empty` is used for `ParentId = null`.
- Lazy task wrapper ensures concurrent requests for the same key share a single build.
- TTL fallback: 6 hours. Primary invalidation is event-driven.
- `IUserDataManager.UserDataSaved` handler: when the saved change toggles `Played`, `IsFavorite`, or `Likes`, remove every cache entry whose key starts with `(userId, item.GetBaseItemKind(), *)`. Other UserData changes (e.g. `PlaybackPositionTicks` updates) are ignored — they fire constantly and don't affect taste.
- Cache cost: one profile per (user, kind, parentId) — four dictionaries of small strings/Guids — kilobytes per entry. Bounded.

## 10. Controller changes

### `MoviesController` (`Jellyfin.Api/Controllers/MoviesController.cs`)

Today (lines 30-326): the controller declares 4 dependencies and contains `GetMovieRecommendations` + private helpers `GetWithDirector`, `GetWithActor`, `GetSimilarTo`, `GetActors`, `GetDirectors` — about 260 lines.

After: constructor gains `IRecommendationsService recommendationsService`. The 260 lines of private helpers and the body of `GetMovieRecommendations` are deleted. The new body is ~15 lines:

```csharp
[HttpGet("Recommendations")]
public async Task<ActionResult<IEnumerable<RecommendationDto>>> GetMovieRecommendations(
    [FromQuery] Guid? userId,
    [FromQuery] Guid? parentId,
    [FromQuery, ModelBinder(typeof(CommaDelimitedCollectionModelBinder))] ItemFields[] fields,
    [FromQuery] int categoryLimit = 5,
    [FromQuery] int itemLimit = 8,
    CancellationToken cancellationToken = default)
{
    userId = RequestHelpers.GetUserId(User, userId);
    var request = new RecommendationRequest(
        userId.Value, BaseItemKind.Movie, parentId,
        categoryLimit, itemLimit, new DtoOptions { Fields = fields });
    var result = await _recommendationsService
        .GetRecommendationsAsync(request, cancellationToken).ConfigureAwait(false);
    return Ok(result);
}
```

Route, params, response DTO unchanged. The controller has no other actions (the entire current file is `GetMovieRecommendations` + its five private helpers), so `_userManager`, `_libraryManager`, `_dtoService`, `_serverConfigurationManager` and their constructor parameters are all removed. The new constructor takes only `IRecommendationsService`.

### `SuggestionsController` (`Jellyfin.Api/Controllers/SuggestionsController.cs`)

Today: `OrderBy = Random` is the entire ranking. After:

```csharp
// After building the InternalItemsQuery, but before executing:
if (RecommendationsService.TryGetRecommendableKind(type, mediaType, out var kind))
{
    var ranked = await _recommendationsService
        .GetRankedItemsAsync(userId.Value, kind, startIndex, limit, dtoOptions, cancellationToken)
        .ConfigureAwait(false);
    return ranked;
}
// else fall through to existing random behavior
```

`TryGetRecommendableKind` is a static helper on the service: returns true only when the caller asks for a single recommendable kind (`Movie` or `Series`). Mixed types, photos, audio, etc. take the random fallback. The obsolete `GetSuggestionsLegacy` route at lines 114-125 delegates to `GetSuggestions` and needs no separate change.

### DI registration (`Emby.Server.Implementations/ApplicationHost.cs`)

One line added immediately after the existing `ISimilarItemsManager` registration at line 551:

```csharp
serviceCollection.AddSingleton<IRecommendationsService, RecommendationsService>();
```

## 11. Edge cases and error handling

- **Cold start (`TotalSignalMass == 0`):** `RecommendationsService` returns an empty `RecommendationDto[]` from the Recommendations endpoint. Empty is correct — the home-screen carousel should not render until the user has signaled taste. `SuggestionsController` falls back to its existing random behavior in the cold-start case, matching today's empty-history experience.
- **`ParentId` scoping:** both profile build and candidate queries pass `ParentId` + `Recursive = true`. Profile is keyed `(userId, kind, parentId)` so different libraries can have different profiles for the same user (a Kids library should not inherit the user's Adult-movie profile).
- **External content (`EnableExternalContentInSuggestions`):** preserved. When set, `Trailer` and `LiveTvProgram` are appended to `IncludeItemTypes` exactly as today.
- **Concurrent profile builds:** `Lazy<Task<TasteProfile>>` ensures one build per key.
- **DB failure during profile build:** logged at warning. Cache stores an empty profile briefly (60 seconds) to avoid retry storms. Caller still gets a valid (empty) response.
- **Item with no metadata:** contributes nothing to the profile; scores 0 against any profile; falls out of recommendations naturally.
- **Deleted seed item between cache and use:** `GetItemById` returns null; skip and continue to the next seed.
- **`userId` not provided:** preserves existing behavior — fall back to `RequestHelpers.GetUserId(User, userId)` from the auth context.

## 12. Testing strategy

New tests in existing test projects. No new test projects added.

### Unit tests (no DB, no DI)

**`tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/TasteProfileBuilderTests.cs`:**
- Empty user history -> empty profile, `TotalSignalMass == 0`.
- Single watched movie -> expected genre/tag/person/studio weights.
- Same movie watched and favorited -> signal stacks (3.0, not 1.0 or 2.0).
- Item with 10 listed people -> only top 5 by `SortOrder` are in the profile.
- 600 watched items -> only 500 most-recent contribute (capped).
- Per-kind isolation: Movie profile does not include data from a Series.

**`tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/TasteProfileScorerTests.cs`:**
- Zero-overlap candidate -> score < high-overlap candidate.
- Identical metadata to seed -> high score with seed bonus applied.
- Profile with no signal mass -> all candidates score equally (no divide-by-zero).
- Same candidate, same profile, with vs. without seed -> seed bonus measurably present.

### Service tests (mocked `ILibraryManager`, `IUserDataManager`, `IPeopleRepository`)

**`tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/RecommendationsServiceTests.cs`:**
- Cold-start request returns empty `RecommendationDto[]`.
- Returns categories of expected `RecommendationType` values (SimilarToRecentlyPlayed, HasDirectorFromRecentlyPlayed, etc.) given a fixture history.
- Categories with fewer than `itemLimit / 2` items are skipped.
- Already-watched items never appear in results.
- `UserDataSaved` event with `Played` change invalidates cache; subsequent request triggers a rebuild.
- `UserDataSaved` event with only `PlaybackPositionTicks` change does NOT invalidate cache.
- Concurrent requests for the same `(user, kind)` trigger one builder execution.

### Controller tests

**`tests/Jellyfin.Api.Tests/Controllers/MoviesControllerTests.cs`** (new file):
- `GetMovieRecommendations` returns 200 with `IEnumerable<RecommendationDto>` shape.
- Respects `categoryLimit` and `itemLimit`.
- Returns empty array for user with no history.

**`tests/Jellyfin.Api.Tests/Controllers/SuggestionsControllerTests.cs`** (new file):
- Falls back to random for mixed/unrecommendable types.
- Ranks by profile when single recommendable type requested.

### Out of scope for v1 tests

- End-to-end / live-DB tests — not run in CI today, keep CI cost flat.
- Frontend tests — the frontend is in a separate repo (jellyfin-web).

## 13. Files touched (summary)

**New files (6):**
- `MediaBrowser.Controller/Library/Recommendations/IRecommendationsService.cs`
- `MediaBrowser.Controller/Library/Recommendations/TasteProfile.cs`
- `MediaBrowser.Controller/Library/Recommendations/RecommendationRequest.cs`
- `Emby.Server.Implementations/Library/Recommendations/RecommendationsService.cs`
- `Emby.Server.Implementations/Library/Recommendations/TasteProfileBuilder.cs`
- `Emby.Server.Implementations/Library/Recommendations/TasteProfileScorer.cs`

**Modified files (5):**
- `Jellyfin.Api/Controllers/MoviesController.cs` — delete ~260 lines, add ~15; constructor reduces from 4 dependencies to 1.
- `Jellyfin.Api/Controllers/SuggestionsController.cs` — add one conditional delegation in `GetSuggestions`; constructor gains `IRecommendationsService`. The legacy `GetSuggestionsLegacy` route is unchanged (it already delegates).
- `Emby.Server.Implementations/ApplicationHost.cs` — one DI line after line 551.
- `MediaBrowser.Controller/Entities/InternalPeopleQuery.cs` — add `public IReadOnlyList<Guid> ItemIds { get; set; }` (additive, existing `ItemId` field unchanged).
- `Jellyfin.Server.Implementations/Item/PeopleRepository.cs` — honor the new `ItemIds` filter in `GetPeople`. When set, the EF query uses `Where(p => itemIds.Contains(p.ItemId))` instead of `Where(p => p.ItemId == singleId)`. If both `ItemId` and `ItemIds` are set, `ItemId` takes precedence (back-compat).

**New test files (5):**
- `tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/TasteProfileBuilderTests.cs`
- `tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/TasteProfileScorerTests.cs`
- `tests/Jellyfin.Server.Implementations.Tests/Library/Recommendations/RecommendationsServiceTests.cs`
- `tests/Jellyfin.Api.Tests/Controllers/MoviesControllerTests.cs`
- `tests/Jellyfin.Api.Tests/Controllers/SuggestionsControllerTests.cs`

**Unchanged (explicit non-touches):**
- `SimilarItemsManager`, `MovieSimilarItemsProvider`, `SeriesSimilarItemsProvider`, all TMDB / ListenBrainz providers.
- `LibraryController` and `/Items/{itemId}/Similar` route.
- `InternalItemsQuery`, `UserDataManager` (only event subscription added externally).
- `BaseItem`, `UserData` and any database schema. The `InternalPeopleQuery` change is a query-builder DTO addition only, not a schema change.
- All client / frontend code.

## 14. Success criteria

1. `/Movies/Recommendations` for a user with watch history returns categories whose items genuinely share genre/tag/people/studio metadata with the named baseline, ranked by metadata overlap rather than randomly. Verified by an integration test fixture and by manual inspection on a real library.
2. `/Movies/Recommendations` for a user with no history returns an empty list, not a list of random items.
3. `/Items/Suggestions?mediaType=Movie` returns items ranked by alignment with the user's overall movie taste profile.
4. Same behaviors for `Series` via the appropriate request parameters.
5. All new unit, service, and controller tests pass in CI.
6. No regression in `/Items/{itemId}/Similar` ("More Like This") — still produced by the unchanged `SimilarItemsManager` pipeline.
7. No DB schema migration required.
