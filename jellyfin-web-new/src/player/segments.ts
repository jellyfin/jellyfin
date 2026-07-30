import type {
    MediaSegment,
    PlaybackHttpClient,
    PlaybackPreferences,
    ProfilePlaybackSettings
} from './types';

const MIN_PROMPT_TICKS = 3 * 10_000_000;
const ALLOWED_TYPES = new Set([ 'Intro', 'Recap', 'Outro' ]);

export function normalizeSegments(
    segments: MediaSegment[],
    settings: ProfilePlaybackSettings,
    preferences: PlaybackPreferences
) {
    return segments
        .filter(segment => {
            if (!segment.Type || !ALLOWED_TYPES.has(segment.Type)) return false;
            if (segment.Type === 'Intro' && !settings.SkipIntroEnabled) return false;
            if (segment.Type === 'Recap' && !settings.SkipRecapEnabled) return false;
            if (segment.Type === 'Outro' && !preferences.SkipCreditsEnabled) return false;

            const start = segment.StartTicks;
            const end = segment.EndTicks;
            return Number.isFinite(start)
                && Number.isFinite(end)
                && (end ?? 0) - (start ?? 0) >= MIN_PROMPT_TICKS;
        })
        .sort((left, right) => (left.StartTicks ?? 0) - (right.StartTicks ?? 0));
}

export function findActiveSegment(segments: MediaSegment[], positionSeconds: number) {
    const ticks = positionSeconds * 10_000_000;
    return segments.find(segment =>
        ticks >= (segment.StartTicks ?? Number.POSITIVE_INFINITY)
        && ticks < (segment.EndTicks ?? Number.NEGATIVE_INFINITY));
}

export async function loadMediaSegments(
    client: PlaybackHttpClient,
    itemId: string,
    settings: ProfilePlaybackSettings,
    preferences: PlaybackPreferences,
    signal?: AbortSignal
) {
    const query = [ 'Intro', 'Recap', 'Outro' ]
        .map(type => `includeSegmentTypes=${type}`)
        .join('&');
    try {
        const result = await client.request<{ Items?: MediaSegment[] }>(
            `MediaSegments/${encodeURIComponent(itemId)}?${query}`,
            signal ? { signal } : undefined
        );
        return normalizeSegments(result.Items ?? [], settings, preferences);
    } catch {
        // No segment provider/plugin is a supported state.
        return [];
    }
}
