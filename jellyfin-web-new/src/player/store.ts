import { useSyncExternalStore } from 'react';

import type { PlayerSnapshot } from './types';

const initialState: PlayerSnapshot = {
    status: 'idle',
    currentTime: 0,
    duration: 0,
    volume: 1,
    playbackRate: 1
};

let snapshot = initialState;
const listeners = new Set<() => void>();

export const playerStore = {
    getSnapshot: () => snapshot,
    subscribe(listener: () => void) {
        listeners.add(listener);
        return () => listeners.delete(listener);
    },
    set(patch: Partial<PlayerSnapshot>) {
        snapshot = { ...snapshot, ...patch };
        listeners.forEach(listener => listener());
    },
    reset() {
        snapshot = initialState;
        listeners.forEach(listener => listener());
    },
    isActive() {
        return snapshot.status === 'loading'
            || snapshot.status === 'playing'
            || snapshot.status === 'paused';
    }
};

export function usePlayerState() {
    return useSyncExternalStore(
        playerStore.subscribe,
        playerStore.getSnapshot,
        playerStore.getSnapshot
    );
}
