import { describe, expect, it } from 'vitest';

import { SessionStorage } from './storage';

describe('SessionStorage', () => {
    it('namespaces sessions per server and keeps a stable device id', () => {
        const storage = new MapStorage();
        const first = new SessionStorage('https://one/emby', storage, () => 'device-1');
        const second = new SessionStorage('https://two', storage, () => 'device-2');

        first.setSession({ accessToken: 'secret', userId: 'user-1' });
        expect(first.getSession()).toEqual({ accessToken: 'secret', userId: 'user-1' });
        expect(second.getSession()).toBeNull();
        expect(first.getDeviceId()).toBe('device-1');
        expect(first.getDeviceId()).toBe('device-1');

        first.clearSession();
        expect(first.getSession()).toBeNull();
        expect(first.getDeviceId()).toBe('device-1');
    });

    it('discards malformed local data', () => {
        const storage = new MapStorage();
        storage.setItem(
            'jellyfin-web-new:https%3A%2F%2Fmedia:session',
            '{"accessToken":42}'
        );

        const sessions = new SessionStorage('https://media', storage);
        expect(sessions.getSession()).toBeNull();
        expect(storage.length).toBe(0);
    });
});

class MapStorage implements Storage {
    private readonly values = new Map<string, string>();

    public get length() {
        return this.values.size;
    }

    public clear() {
        this.values.clear();
    }

    public getItem(key: string) {
        return this.values.get(key) ?? null;
    }

    public key(index: number) {
        return [...this.values.keys()][index] ?? null;
    }

    public removeItem(key: string) {
        this.values.delete(key);
    }

    public setItem(key: string, value: string) {
        this.values.set(key, value);
    }
}
