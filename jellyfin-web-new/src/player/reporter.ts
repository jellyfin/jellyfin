import type {
    CustomProgressReport,
    NativePlaybackReport,
    PlaybackHttpClient,
    PlayerItem,
    PlaybackSelection
} from './types';

export const PROGRESS_INTERVAL_MS = 10_000;
const START_RETRY_DELAYS_MS = [ 250, 750 ] as const;
const AUTH_QUERY_KEYS = new Set([
    'api_key',
    'apikey',
    'token',
    'access_token',
    'x-emby-token'
]);

export const shouldReportProgress = (lastReportAt: number, now: number, force = false) =>
    force || now - lastReportAt >= PROGRESS_INTERVAL_MS;

interface ProgressState {
    currentTime: number;
    duration: number;
    paused: boolean;
    muted: boolean;
    volume: number;
    playbackRate: number;
}

interface ReporterOptions {
    client: PlaybackHttpClient;
    item: PlayerItem;
    profileId: string;
    playSessionId?: string | null | undefined;
    selection: PlaybackSelection;
    readState: () => ProgressState;
    onSessionExpired?: (() => void) | undefined;
}

const asJson = (body: unknown, signal: AbortSignal) => ({
    body: JSON.stringify(body),
    headers: { 'Content-Type': 'application/json' },
    method: 'POST',
    signal
} satisfies RequestInit);

function stripAuthQuery(url: string) {
    const safe = new URL(url, window.location.href);
    [ ...safe.searchParams.keys() ].forEach(key => {
        if (AUTH_QUERY_KEYS.has(key.toLowerCase())) safe.searchParams.delete(key);
    });
    return safe.toString();
}

const errorStatus = (error: unknown) =>
    (error as { status?: number; response?: { status?: number } })?.status
    ?? (error as { response?: { status?: number } })?.response?.status;

const isTransientStartError = (error: unknown) => {
    const status = errorStatus(error);
    return status == null
        || status === 0
        || status === 408
        || status === 425
        || status === 429
        || status >= 500;
};

export class ProgressReporter {
    private readonly abortController = new AbortController();
    private lastReportAt = 0;
    private sessionExpired = false;
    private startPromise: Promise<void> | undefined;
    private timer: number | undefined;
    private stopped = false;

    constructor(private readonly options: ReporterOptions) {}

    setSubtitleStreamIndex(index: number | undefined) {
        this.options.selection.subtitleStreamIndex = index;
    }

    private nativeReport(): NativePlaybackReport {
        const state = this.options.readState();
        return {
            ItemId: this.options.item.Id,
            MediaSourceId: this.options.selection.source.Id,
            PlaySessionId: this.options.playSessionId,
            PositionTicks: Math.max(0, Math.round(state.currentTime * 10_000_000)),
            IsPaused: state.paused,
            IsMuted: state.muted,
            VolumeLevel: Math.round(state.volume * 100),
            PlayMethod: this.options.selection.method,
            AudioStreamIndex: this.options.selection.audioStreamIndex,
            SubtitleStreamIndex: this.options.selection.subtitleStreamIndex,
            PlaybackRate: state.playbackRate,
            CanSeek: true
        };
    }

    private customReport(): CustomProgressReport | null {
        const state = this.options.readState();
        if (!Number.isFinite(state.duration) || state.duration <= 0) return null;
        return {
            ItemId: this.options.item.Id,
            MediaSourceId: this.options.selection.source.Id,
            PositionSeconds: Math.max(0, Math.min(state.currentTime, state.duration)),
            DurationSeconds: state.duration,
            IsPaused: state.paused,
            PlaySessionId: this.options.playSessionId,
            ClientName: 'Jellyfin Web New'
        };
    }

    start(): Promise<void> {
        if (this.stopped) return Promise.resolve();
        if (this.startPromise) return this.startPromise;

        document.addEventListener('visibilitychange', this.onVisibilityChange);
        window.addEventListener('pagehide', this.onPageHide);
        this.startPromise = this.startReporting();
        return this.startPromise;
    }

    private async startReporting() {
        try {
            for (let attempt = 0; ; attempt += 1) {
                try {
                    await this.options.client.request(
                        'Sessions/Playing',
                        asJson(this.nativeReport(), this.abortController.signal)
                    );
                    break;
                } catch (error) {
                    if (this.stopped) return;
                    if (errorStatus(error) === 401) {
                        this.notifySessionExpired();
                        throw error;
                    }
                    const retryDelay = START_RETRY_DELAYS_MS[attempt];
                    if (retryDelay === undefined || !isTransientStartError(error)) {
                        throw error;
                    }
                    if (!await this.waitForRetry(retryDelay)) return;
                }
            }
            if (this.stopped) return;
            await this.report(true);
            if (this.stopped || this.sessionExpired) return;
            this.timer = window.setInterval(() => void this.report(), PROGRESS_INTERVAL_MS);
        } catch (error) {
            if (this.stopped) return;
            this.removeListeners();
            this.startPromise = undefined;
            throw error;
        }
    }

    async report(force = false) {
        if (this.stopped) return false;
        const now = Date.now();
        if (!shouldReportProgress(this.lastReportAt, now, force)) return false;
        this.lastReportAt = now;
        const custom = this.customReport();
        const requests: Promise<unknown>[] = [
            this.options.client.request(
                'Sessions/Playing/Progress',
                asJson(this.nativeReport(), this.abortController.signal)
            )
        ];
        if (custom) {
            requests.push(this.options.client.request(
                `CustomNetflix/v1/profiles/${encodeURIComponent(this.options.profileId)}/progress`,
                asJson(custom, this.abortController.signal)
            ));
        }
        const results = await Promise.allSettled(requests);
        const rejected = results.find(result => result.status === 'rejected');
        if (rejected) {
            if (errorStatus(rejected.reason) === 401) this.notifySessionExpired();
            this.lastReportAt = 0;
            return false;
        }
        return true;
    }

    private notifySessionExpired() {
        if (this.sessionExpired) return;
        this.sessionExpired = true;
        this.options.onSessionExpired?.();
    }

    private waitForRetry(delayMs: number) {
        return new Promise<boolean>(resolve => {
            const signal = this.abortController.signal;
            if (this.stopped || signal.aborted) {
                resolve(false);
                return;
            }

            const onAbort = () => {
                window.clearTimeout(timer);
                resolve(false);
            };
            const timer = window.setTimeout(() => {
                signal.removeEventListener('abort', onAbort);
                resolve(true);
            }, delayMs);
            signal.addEventListener('abort', onAbort, { once: true });
        });
    }

    private onVisibilityChange = () => {
        void this.report(true);
    };

    private onPageHide = () => {
        this.stop();
    };

    private removeListeners() {
        document.removeEventListener('visibilitychange', this.onVisibilityChange);
        window.removeEventListener('pagehide', this.onPageHide);
    }

    stop() {
        if (this.stopped) return;
        this.stopped = true;
        this.abortController.abort();
        if (this.timer !== undefined) {
            window.clearInterval(this.timer);
            this.timer = undefined;
        }
        this.removeListeners();

        const headers = new Headers(this.options.client.authHeaders());
        headers.set('Content-Type', 'application/json');
        const send = (path: string, body: unknown) => {
            const url = stripAuthQuery(this.options.client.url(path));
            void fetch(url, {
                body: JSON.stringify(body),
                credentials: 'same-origin',
                headers,
                keepalive: true,
                method: 'POST'
            }).catch(() => undefined);
        };
        send('Sessions/Playing/Stopped', this.nativeReport());
        const custom = this.customReport();
        if (custom) {
            send(
                `CustomNetflix/v1/profiles/${encodeURIComponent(this.options.profileId)}/progress`,
                custom
            );
        }
    }
}
