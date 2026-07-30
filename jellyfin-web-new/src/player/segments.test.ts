import { describe, expect, it, vi } from 'vitest';

import {
    findActiveSegment,
    loadMediaSegments,
    normalizeSegments
} from './segments';
import type {
    PlaybackHttpClient,
    PlaybackPreferences,
    ProfilePlaybackSettings
} from './types';

const settings: ProfilePlaybackSettings = {
    AutoplayEnabled: true,
    AutoplayDelaySeconds: 8,
    SkipIntroEnabled: true,
    SkipRecapEnabled: false
};
const preferences: PlaybackPreferences = {
    PreferDirectPlay: true,
    AllowContainerRemuxing: true,
    AllowVideoTranscoding: true,
    AllowAudioTranscoding: true,
    PreferHardwareTranscoding: true,
    SubtitlesEnabled: false,
    AudioDescriptionEnabled: false,
    ClosedCaptionsEnabled: false,
    SkipCreditsEnabled: true
};

describe('media segments', () => {
    it('keeps enabled segments lasting at least three seconds', () => {
        const result = normalizeSegments([
            { Type: 'Intro', StartTicks: 10_000_000, EndTicks: 50_000_000 },
            { Type: 'Recap', StartTicks: 0, EndTicks: 80_000_000 },
            { Type: 'Credits', StartTicks: 100_000_000, EndTicks: 150_000_000 },
            { Type: 'Outro', StartTicks: 200_000_000, EndTicks: 240_000_000 }
        ], settings, preferences);

        expect(result.map(segment => segment.Type)).toEqual([ 'Intro', 'Outro' ]);
        expect(findActiveSegment(result, 2)?.Type).toBe('Intro');
        expect(findActiveSegment(result, 7)).toBeUndefined();
    });

    it('requests only segment values accepted by the native API', async () => {
        const request = vi.fn().mockResolvedValue({ Items: [] });
        const client = {
            request
        } as unknown as PlaybackHttpClient;

        await loadMediaSegments(client, 'item id', settings, preferences);

        expect(request).toHaveBeenCalledWith(
            'MediaSegments/item%20id?includeSegmentTypes=Intro'
                + '&includeSegmentTypes=Recap&includeSegmentTypes=Outro',
            undefined
        );
    });
});
