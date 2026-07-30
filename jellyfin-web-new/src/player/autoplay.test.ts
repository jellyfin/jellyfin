import { describe, expect, it, vi } from 'vitest';

import {
    buildAutoplayAction,
    buildAutoplayDecision,
    getAutoplayAction
} from './autoplay';
import type { NextEpisode, PlaybackHttpClient } from './types';

describe('buildAutoplayDecision', () => {
    it('clamps the delay and preserves still-watching state', () => {
        expect(buildAutoplayDecision({
            HasNext: true,
            DelaySeconds: 120,
            Item: { Id: 'episode-2', Name: 'Episode 2' },
            ResumePositionSeconds: -1,
            Reason: 'next',
            RequiresStillWatchingConfirmation: true
        })).toEqual({
            delaySeconds: 60,
            itemId: 'episode-2',
            name: 'Episode 2',
            requiresConfirmation: true,
            resumePositionSeconds: 0
        });
    });

    it('returns null without a playable next item', () => {
        expect(buildAutoplayDecision({
            HasNext: false,
            DelaySeconds: 8,
            Reason: 'series-ended',
            RequiresStillWatchingConfirmation: false,
            ResumePositionSeconds: 0
        })).toBeNull();
    });

    it('preserves a confirmation-only backend response', () => {
        expect(buildAutoplayAction({
            HasNext: false,
            DelaySeconds: 8,
            Reason: 'still-watching',
            RequiresStillWatchingConfirmation: true,
            ResumePositionSeconds: 0
        })).toEqual({
            type: 'confirm',
            pendingDecision: null
        });
    });

    it('keeps a playable decision pending until confirmation', () => {
        const action = buildAutoplayAction({
            HasNext: true,
            DelaySeconds: 8,
            Item: { Id: 'episode-2', Name: 'Episode 2' },
            Reason: 'still-watching',
            RequiresStillWatchingConfirmation: true,
            ResumePositionSeconds: 0
        });

        expect(action).toMatchObject({
            type: 'confirm',
            pendingDecision: { itemId: 'episode-2' }
        });
    });

    it('can refetch the autoplay state when playback actually ends', async () => {
        const response: NextEpisode = {
            HasNext: true,
            DelaySeconds: 8,
            Item: { Id: 'episode-2', Name: 'Episode 2' },
            Reason: 'next',
            RequiresStillWatchingConfirmation: false,
            ResumePositionSeconds: 0
        };
        const request = vi.fn().mockResolvedValue(response);
        const client = { request } as unknown as PlaybackHttpClient;

        await getAutoplayAction(client, 'profile id', 'episode/1');
        await getAutoplayAction(client, 'profile id', 'episode/1');

        expect(request).toHaveBeenCalledTimes(2);
        expect(request).toHaveBeenLastCalledWith(
            'CustomNetflix/v1/items/episode%2F1/next-episode?profileId=profile%20id',
            undefined
        );
    });
});
