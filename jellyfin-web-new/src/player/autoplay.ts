import type { NextEpisode, PlaybackHttpClient } from './types';

export interface AutoplayDecision {
    delaySeconds: number;
    itemId: string;
    name: string;
    requiresConfirmation: boolean;
    resumePositionSeconds: number;
}

export type AutoplayAction =
    | {
        type: 'confirm';
        pendingDecision: AutoplayDecision | null;
    }
    | {
        type: 'play-next';
        decision: AutoplayDecision;
    };

export function buildAutoplayDecision(next: NextEpisode): AutoplayDecision | null {
    const itemId = next.Item?.Id;
    if (!next.HasNext || !itemId) return null;

    return {
        delaySeconds: Math.max(0, Math.min(60, Math.round(next.DelaySeconds || 0))),
        itemId,
        name: next.Item?.Name || '',
        requiresConfirmation: next.RequiresStillWatchingConfirmation,
        resumePositionSeconds: Math.max(0, next.ResumePositionSeconds || 0)
    };
}

export function buildAutoplayAction(next: NextEpisode): AutoplayAction | null {
    const decision = buildAutoplayDecision(next);
    if (next.RequiresStillWatchingConfirmation) {
        return {
            type: 'confirm',
            pendingDecision: decision
        };
    }
    return decision ? { type: 'play-next', decision } : null;
}

async function requestNextEpisode(
    client: PlaybackHttpClient,
    profileId: string,
    itemId: string,
    signal?: AbortSignal
) {
    return client.request<NextEpisode>(
        `CustomNetflix/v1/items/${encodeURIComponent(itemId)}/next-episode?profileId=${encodeURIComponent(profileId)}`,
        signal ? { signal } : undefined
    );
}

export async function getAutoplayDecision(
    client: PlaybackHttpClient,
    profileId: string,
    itemId: string,
    signal?: AbortSignal
) {
    const next = await requestNextEpisode(client, profileId, itemId, signal);
    return buildAutoplayDecision(next);
}

/**
 * Fetch this at playback end so still-watching state is not based on a stale prefetch.
 */
export async function getAutoplayAction(
    client: PlaybackHttpClient,
    profileId: string,
    itemId: string,
    signal?: AbortSignal
) {
    const next = await requestNextEpisode(client, profileId, itemId, signal);
    return buildAutoplayAction(next);
}

export async function confirmStillWatching(client: PlaybackHttpClient, profileId: string) {
    await client.request(
        `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/autoplay/still-watching/confirm`,
        { method: 'POST' }
    );
}
