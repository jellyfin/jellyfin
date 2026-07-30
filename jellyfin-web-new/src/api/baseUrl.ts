export function deriveApiBaseUrl(input: string | URL): string {
    const url = new URL(input.toString());
    const segments = url.pathname.split('/').filter(Boolean);
    const webIndex = segments.lastIndexOf('web');
    const apiSegments = webIndex >= 0
        ? segments.slice(0, webIndex)
        : segments.at(-1)?.includes('.')
            ? segments.slice(0, -1)
            : segments;

    url.pathname = apiSegments.length ? `/${apiSegments.join('/')}` : '';
    url.search = '';
    url.hash = '';

    return url.toString().replace(/\/$/, '');
}

export function getDefaultApiBaseUrl(location = window.location): string {
    return import.meta.env.DEV
        ? `${location.origin}/jellyfin-api`
        : deriveApiBaseUrl(location.href);
}
