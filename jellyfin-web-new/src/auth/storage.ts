import type { Profile } from '../api/profiles';

export interface StoredSession {
    accessToken: string;
    activeProfile?: Profile;
    serverId?: string;
    sessionId?: string;
    userId: string;
}

const PREFIX = 'jellyfin-web-new';

function isSession(value: unknown): value is StoredSession {
    if (!value || typeof value !== 'object') return false;
    const session = value as Partial<StoredSession>;
    return typeof session.accessToken === 'string'
        && session.accessToken.length > 0
        && typeof session.userId === 'string'
        && session.userId.length > 0
        && (session.activeProfile === undefined
            || (typeof session.activeProfile === 'object'
                && session.activeProfile !== null
                && typeof session.activeProfile.Id === 'string'
                && typeof session.activeProfile.Name === 'string'
                && typeof session.activeProfile.JellyfinUserId === 'string'))
        && (session.serverId === undefined || typeof session.serverId === 'string')
        && (session.sessionId === undefined || typeof session.sessionId === 'string');
}

export class SessionStorage {
    private readonly namespace: string;

    public constructor(
        baseUrl: string,
        private readonly storage: Storage = window.localStorage,
        private readonly createId: () => string = () => crypto.randomUUID()
    ) {
        this.namespace = `${PREFIX}:${encodeURIComponent(baseUrl.replace(/\/$/, ''))}`;
    }

    public getDeviceId(): string {
        const key = `${this.namespace}:device-id`;
        const existing = this.storage.getItem(key);
        if (existing) return existing;

        const deviceId = this.createId();
        this.storage.setItem(key, deviceId);
        return deviceId;
    }

    public getSession(): StoredSession | null {
        const key = `${this.namespace}:session`;
        const serialized = this.storage.getItem(key);
        if (!serialized) return null;

        try {
            const session: unknown = JSON.parse(serialized);
            if (isSession(session)) return session;
        } catch {
            // Invalid local data is discarded below.
        }

        this.storage.removeItem(key);
        return null;
    }

    public setSession(session: StoredSession): void {
        this.storage.setItem(`${this.namespace}:session`, JSON.stringify(session));
    }

    public clearSession(): void {
        this.storage.removeItem(`${this.namespace}:session`);
    }
}
