import {
    createStore,
    del,
    get,
    keys,
    set
} from 'idb-keyval';

const store = createStore('jellyfin-web-new', 'metadata-snapshots');
const VERSION = 'v1';
const TTL_MS = 24 * 60 * 60 * 1000;
const MAX_SNAPSHOTS_PER_PROFILE = 24;

export type SnapshotKind = 'home' | 'title';

interface Snapshot<T> {
    expiresAt: number;
    savedAt: number;
    value: T;
}

const scope = (userId: string, profileId: string) =>
    `${VERSION}:${encodeURIComponent(userId)}:${encodeURIComponent(profileId)}:`;

const snapshotKey = (
    userId: string,
    profileId: string,
    kind: SnapshotKind,
    id = 'default'
) => `${scope(userId, profileId)}${kind}:${encodeURIComponent(id)}`;

export async function readMetadataSnapshot<T>(
    userId: string,
    profileId: string,
    kind: SnapshotKind,
    id?: string
) {
    const key = snapshotKey(userId, profileId, kind, id);
    const snapshot = await get<Snapshot<T>>(key, store);
    if (!snapshot) return null;
    if (snapshot.expiresAt <= Date.now()) {
        await del(key, store);
        return null;
    }
    return snapshot.value;
}

export async function writeMetadataSnapshot<T>(
    userId: string,
    profileId: string,
    kind: SnapshotKind,
    value: T,
    id?: string
) {
    const now = Date.now();
    await set(snapshotKey(userId, profileId, kind, id), {
        expiresAt: now + TTL_MS,
        savedAt: now,
        value
    } satisfies Snapshot<T>, store);

    const prefix = scope(userId, profileId);
    const profileKeys = (await keys(store))
        .filter((key): key is string => typeof key === 'string' && key.startsWith(prefix));
    if (profileKeys.length <= MAX_SNAPSHOTS_PER_PROFILE) return;

    const snapshots = await Promise.all(profileKeys.map(async key => ({
        key,
        savedAt: (await get<Snapshot<unknown>>(key, store))?.savedAt ?? 0
    })));
    await Promise.all(
        snapshots
            .sort((left, right) => right.savedAt - left.savedAt)
            .slice(MAX_SNAPSHOTS_PER_PROFILE)
            .map(({ key }) => del(key, store))
    );
}

export async function clearMetadataSnapshots(userId?: string, profileId?: string) {
    const prefix = userId
        ? profileId
            ? scope(userId, profileId)
            : `${VERSION}:${encodeURIComponent(userId)}:`
        : `${VERSION}:`;
    const matching = (await keys(store))
        .filter(key => typeof key === 'string' && key.startsWith(prefix));
    await Promise.all(matching.map(key => del(key, store)));
}
