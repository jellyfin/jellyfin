import {
    createContext,
    type PropsWithChildren,
    useContext,
    useMemo
} from 'react';
import { Navigate, Outlet } from 'react-router-dom';

import { ApiError } from '../api';
import { useSession } from '../auth';
import {
    CatalogProvider,
    type CatalogClient,
    type CatalogRequest,
    type ImageKind,
    type MediaItem
} from '../features/catalog/catalog';
import { useProfile } from '../features/profiles';
import { useI18n } from '../i18n';
import type { PlaybackHttpClient } from '../player';
import { LoadingPage } from '../pages/SystemPages';

function apiUrl(baseUrl: string, path: string) {
    if (/^https?:\/\//i.test(path)) return new URL(path);
    return new URL(path.replace(/^\/+/, ''), `${baseUrl.replace(/\/+$/, '')}/`);
}

function appendQuery(
    url: URL,
    query: Record<string, boolean | number | string | null | undefined> = {}
) {
    Object.entries(query).forEach(([ key, value ]) => {
        if (value !== null && value !== undefined) {
            url.searchParams.set(key, String(value));
        }
    });
    return url.toString();
}

export function buildMediaUrl(
    baseUrl: string,
    path: string,
    query: Record<string, boolean | number | string | null | undefined> = {},
    accessToken?: string
) {
    const url = apiUrl(baseUrl, path);
    const safeQuery = { ...query };
    if (url.origin === new URL(baseUrl).origin) {
        safeQuery.api_key = accessToken;
    } else {
        Object.keys(safeQuery).forEach(key => {
            if ([ 'access_token', 'api_key', 'apikey', 'token', 'x-emby-token' ]
                .includes(key.toLowerCase())) {
                delete safeQuery[key];
            }
        });
    }
    return appendQuery(url, safeQuery);
}

export function RuntimeProvider({ children }: PropsWithChildren) {
    const { client, session } = useSession();
    const { activeProfile, isLoading } = useProfile();
    const { locale } = useI18n();
    const catalogClient = useMemo<CatalogClient>(() => ({
        imageUrl(item: MediaItem, kind: ImageKind, width: number) {
            const useParent = kind === 'Backdrop' && !item.BackdropImageTags?.length && item.ParentBackdropItemId;
            const itemId = useParent ? item.ParentBackdropItemId : item.Id;
            const tag = kind === 'Backdrop'
                ? item.BackdropImageTags?.[0] ?? item.ParentBackdropImageTags?.[0]
                : item.ImageTags?.Primary;
            if (!itemId || !tag) return undefined;
            return appendQuery(apiUrl(client.baseUrl, `Items/${encodeURIComponent(itemId)}/Images/${kind}`), {
                api_key: session?.accessToken,
                maxWidth: width,
                quality: 90,
                tag
            });
        },
        request<T>(path: string, request: CatalogRequest = {}) {
            if (!navigator.onLine && request.method && request.method !== 'GET') {
                return Promise.reject(new ApiError('network'));
            }
            return client.request<T>(request.method ?? 'GET', path, {
                ...(request.body === undefined ? {} : { body: request.body }),
                ...(request.signal ? { signal: request.signal } : {})
            });
        }
    }), [ client, session?.accessToken ]);

    const playbackClient = useMemo<PlaybackHttpClient>(() => ({
        authHeaders() {
            return { Authorization: client.sdkApi.authorizationHeader };
        },
        mediaUrl(path, query) {
            return buildMediaUrl(client.baseUrl, path, query, session?.accessToken);
        },
        async request<T>(path: string, init: RequestInit = {}) {
            let body: unknown;
            if (typeof init.body === 'string' && init.body.length) {
                body = JSON.parse(init.body) as unknown;
            }
            const method = (init.method ?? 'GET').toUpperCase() as 'DELETE' | 'GET' | 'PATCH' | 'POST' | 'PUT';
            if (!navigator.onLine && method !== 'GET') throw new ApiError('network');
            return client.request<T>(method, path, {
                ...(body === undefined ? {} : { body }),
                ...(init.signal ? { signal: init.signal } : {})
            });
        },
        url(path, query) {
            return appendQuery(apiUrl(client.baseUrl, path), query);
        }
    }), [ client, session?.accessToken ]);

    if (isLoading) return <LoadingPage />;
    if (!activeProfile) return <Navigate replace to='/profiles' />;
    if (!session) return <Navigate replace to='/login' />;

    return (
        <CatalogProvider value={{
            client: catalogClient,
            locale,
            profileId: activeProfile.Id,
            userId: session.userId
        }}>
            <PlaybackRuntimeContext.Provider value={playbackClient}>
                {children}
            </PlaybackRuntimeContext.Provider>
        </CatalogProvider>
    );
}

const PlaybackRuntimeContext = createContext<PlaybackHttpClient | null>(null);

export function usePlaybackClient() {
    const value = useContext(PlaybackRuntimeContext);
    if (!value) throw new Error('Playback runtime is missing');
    return value;
}

export function RuntimeOutlet() {
    return <RuntimeProvider><Outlet /></RuntimeProvider>;
}
