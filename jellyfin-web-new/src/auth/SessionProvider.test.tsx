import { renderHook, waitFor } from '@testing-library/react';
import type { PropsWithChildren } from 'react';
import { describe, expect, it } from 'vitest';

import type { JellyfinClient, Profile } from '../api';
import { SessionProvider, useSession } from './SessionProvider';
import { SessionStorage } from './storage';

const activeProfile: Profile = {
    CreatedAt: '2026-07-30T00:00:00Z',
    Id: 'profile-1',
    IsChild: false,
    IsDefault: true,
    JellyfinUserId: 'user-1',
    Name: 'Viewer',
    PlaybackPreferences: {
        AllowAudioTranscoding: true,
        AllowContainerRemuxing: true,
        AllowVideoTranscoding: true,
        AudioDescriptionEnabled: false,
        ClosedCaptionsEnabled: false,
        PreferDirectPlay: true,
        PreferHardwareTranscoding: true,
        SkipCreditsEnabled: false,
        SubtitlesEnabled: false
    },
    Settings: {
        AutoplayDelaySeconds: 8,
        AutoplayEnabled: true,
        SkipIntroEnabled: true,
        SkipRecapEnabled: true
    },
    UpdatedAt: '2026-07-30T00:00:00Z'
};

describe('SessionProvider offline startup', () => {
    it('keeps a stored session and its active profile available offline', async () => {
        const storage = new MapStorage();
        new SessionStorage('https://media.example', storage, () => 'device-1').setSession({
            accessToken: 'token',
            activeProfile,
            userId: 'user-1'
        });
        Object.defineProperty(navigator, 'onLine', { configurable: true, value: false });
        const client = {
            getPublicSystemInfo: () => Promise.reject(new TypeError('offline'))
        } as unknown as JellyfinClient;
        const wrapper = ({ children }: PropsWithChildren) => (
            <SessionProvider baseUrl='https://media.example' client={client} localStorage={storage}>
                {children}
            </SessionProvider>
        );
        const { result } = renderHook(useSession, { wrapper });

        await waitFor(() => expect(result.current.status).toBe('authenticated'));
        expect(result.current.session?.activeProfile?.Id).toBe('profile-1');
    });
});

class MapStorage implements Storage {
    private readonly values = new Map<string, string>();
    public get length() { return this.values.size; }
    public clear() { this.values.clear(); }
    public getItem(key: string) { return this.values.get(key) ?? null; }
    public key(index: number) { return [ ...this.values.keys() ][index] ?? null; }
    public removeItem(key: string) { this.values.delete(key); }
    public setItem(key: string, value: string) { this.values.set(key, value); }
}
