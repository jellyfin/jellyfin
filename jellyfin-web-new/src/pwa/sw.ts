/// <reference lib="webworker" />

declare const self: ServiceWorkerGlobalScope & {
    __WB_MANIFEST: Array<{ revision?: string | null; url: string }>;
};

const CACHE_PREFIX = 'jellyfin-web-new-shell-';
const CACHE_NAME = `${CACHE_PREFIX}v1`;
const PRECACHE_EXTENSION = /\.(?:html|css|js|woff2|svg|png|ico|json)$/i;
const FORBIDDEN_RUNTIME_PATH = /\/(?:Videos|Audio|Sessions|Users|CustomNetflix|Items\/[^/]+\/PlaybackInfo)(?:\/|$)/i;
const precacheUrls = self.__WB_MANIFEST
    .map(entry => entry.url)
    .filter(url => PRECACHE_EXTENSION.test(new URL(url, self.registration.scope).pathname));

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => cache.addAll(precacheUrls))
    );
});

self.addEventListener('activate', event => {
    event.waitUntil(
        caches.keys()
            .then(names => Promise.all(
                names
                    .filter(name => name.startsWith(CACHE_PREFIX) && name !== CACHE_NAME)
                    .map(name => caches.delete(name))
            ))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('message', event => {
    if (event.data?.type === 'SKIP_WAITING') void self.skipWaiting();
});

self.addEventListener('fetch', event => {
    const request = event.request;
    if (request.method !== 'GET') return;
    const url = new URL(request.url);
    if (url.origin !== self.location.origin || FORBIDDEN_RUNTIME_PATH.test(url.pathname)) return;

    if (request.mode === 'navigate') {
        event.respondWith(
            fetch(request).catch(async () => {
                const cache = await caches.open(CACHE_NAME);
                return await cache.match(new URL('index.html', self.registration.scope).toString())
                    ?? Response.error();
            })
        );
        return;
    }

    event.respondWith(
        caches.match(request).then(cached => cached ?? fetch(request))
    );
});

export {};
