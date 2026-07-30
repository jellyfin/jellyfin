import type { PlaybackErrorCode } from './types';

export interface AttachedMedia {
    destroy(): void;
}

export interface AttachMediaOptions {
    maxMediaRecoveries?: number;
    maxNetworkRecoveries?: number;
    startPositionSeconds?: number;
}

const DEFAULT_RECOVERY_WINDOW_MS = 30_000;
const AUTH_QUERY_KEYS = new Set([
    'access_token',
    'api_key',
    'apikey',
    'token',
    'x-emby-token'
]);

const recoveryLimit = (value: number | undefined, fallback: number) =>
    Number.isInteger(value) && (value ?? -1) >= 0 ? value as number : fallback;

const validStartPosition = (value: number | undefined) =>
    Number.isFinite(value) && (value ?? -1) >= 0 ? value : undefined;

const withoutAuthQuery = (url: string) => {
    const safe = new URL(url, window.location.href);
    [ ...safe.searchParams.keys() ].forEach(key => {
        if (AUTH_QUERY_KEYS.has(key.toLowerCase())) safe.searchParams.delete(key);
    });
    return safe.toString();
};

const isSafariOrIosEngine = () => {
    const userAgent = navigator.userAgent;
    const isIos = /iPad|iPhone|iPod/i.test(userAgent)
        || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    const isSafari = /AppleWebKit/i.test(userAgent)
        && /Version\/[\d.]+.*Safari\//i.test(userAgent)
        && !/(?:Chromium|Chrome|CriOS|Edg|EdgiOS|FxiOS|OPR)\//i.test(userAgent);
    return isIos || isSafari;
};

export async function attachMedia(
    video: HTMLVideoElement,
    url: string,
    isHls: boolean,
    headers: HeadersInit,
    onFatalError: (error: PlaybackErrorCode) => void,
    options: AttachMediaOptions = {}
): Promise<AttachedMedia> {
    const startPosition = validStartPosition(options.startPositionSeconds);
    const useNativeHls = isHls
        && isSafariOrIosEngine()
        && Boolean(video.canPlayType('application/vnd.apple.mpegurl'));
    if (!isHls || useNativeHls) {
        video.src = url;
        let destroyed = false;
        const applyStartPosition = () => {
            if (!destroyed && startPosition !== undefined) {
                video.currentTime = startPosition;
            }
        };
        if (startPosition !== undefined) {
            if (video.readyState >= HTMLMediaElement.HAVE_METADATA) {
                applyStartPosition();
            } else {
                video.addEventListener('loadedmetadata', applyStartPosition, { once: true });
            }
        }
        return {
            destroy() {
                if (destroyed) return;
                destroyed = true;
                video.removeEventListener('loadedmetadata', applyStartPosition);
                video.removeAttribute('src');
                video.load();
            }
        };
    }

    const { default: Hls } = await import('hls.js');
    if (!Hls.isSupported()) {
        onFatalError('codec');
        throw new Error('HLS is not supported by this browser');
    }

    const requestHeaders = new Headers(headers);
    const hls = new Hls({
        enableWorker: true,
        lowLatencyMode: false,
        startPosition: startPosition ?? -1,
        xhrSetup(xhr) {
            requestHeaders.forEach((value, key) => xhr.setRequestHeader(key, value));
            xhr.withCredentials = true;
        }
    });
    const maxMediaRecoveries = recoveryLimit(options.maxMediaRecoveries, 2);
    const maxNetworkRecoveries = recoveryLimit(options.maxNetworkRecoveries, 2);
    let destroyed = false;
    let fatal = false;
    const mediaRecoveries: number[] = [];
    const networkRecoveries: number[] = [];
    const consumeRecovery = (recoveries: number[], limit: number) => {
        const now = performance.now();
        while (recoveries.length && now - recoveries[0]! >= DEFAULT_RECOVERY_WINDOW_MS) {
            recoveries.shift();
        }
        if (recoveries.length >= limit) return false;
        recoveries.push(now);
        return true;
    };
    const destroy = () => {
        if (destroyed) return;
        destroyed = true;
        hls.destroy();
    };
    const fail = (code: PlaybackErrorCode) => {
        if (fatal) return;
        fatal = true;
        onFatalError(code);
        destroy();
    };
    hls.on(Hls.Events.ERROR, (_event, data) => {
        if (!data.fatal || fatal || destroyed) return;
        if (data.type === Hls.ErrorTypes.NETWORK_ERROR) {
            if (consumeRecovery(networkRecoveries, maxNetworkRecoveries)) {
                hls.startLoad();
                return;
            }
            fail('network');
            return;
        }
        if (data.type === Hls.ErrorTypes.MEDIA_ERROR) {
            if (consumeRecovery(mediaRecoveries, maxMediaRecoveries)) {
                if (mediaRecoveries.length > 1) hls.swapAudioCodec();
                hls.recoverMediaError();
                return;
            }
            fail('codec');
            return;
        }
        fail('codec');
    });
    // hls.js can authenticate every request with headers; keep credentials out
    // of playlist and segment URLs. Native media elements retain their URL token.
    hls.loadSource(withoutAuthQuery(url));
    hls.attachMedia(video);
    return { destroy };
}
