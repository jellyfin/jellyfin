import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

import { attachMedia } from './hls';

interface HlsDouble {
    config: Record<string, unknown>;
    attachMedia: ReturnType<typeof vi.fn>;
    destroy: ReturnType<typeof vi.fn>;
    emitFatal(type: string): void;
    loadSource: ReturnType<typeof vi.fn>;
    recoverMediaError: ReturnType<typeof vi.fn>;
    startLoad: ReturnType<typeof vi.fn>;
    swapAudioCodec: ReturnType<typeof vi.fn>;
}

const hlsState = vi.hoisted(() => ({
    instances: [] as HlsDouble[]
}));

vi.mock('hls.js', () => {
    class Hls {
        static readonly ErrorTypes = {
            MEDIA_ERROR: 'mediaError',
            NETWORK_ERROR: 'networkError'
        };

        static readonly Events = { ERROR: 'error' };

        static isSupported() {
            return true;
        }

        readonly attachMedia = vi.fn();
        readonly destroy = vi.fn();
        readonly loadSource = vi.fn();
        readonly recoverMediaError = vi.fn();
        readonly startLoad = vi.fn();
        readonly swapAudioCodec = vi.fn();
        private errorHandler?: (
            event: string,
            data: { fatal: boolean; type: string }
        ) => void;

        constructor(readonly config: Record<string, unknown>) {
            hlsState.instances.push(this);
        }

        on(event: string, handler: (
            event: string,
            data: { fatal: boolean; type: string }
        ) => void) {
            if (event === Hls.Events.ERROR) this.errorHandler = handler;
        }

        emitFatal(type: string) {
            this.errorHandler?.(Hls.Events.ERROR, { fatal: true, type });
        }
    }

    return { default: Hls };
});

beforeEach(() => {
    hlsState.instances.length = 0;
});

afterEach(() => {
    vi.useRealTimers();
});

describe('attachMedia', () => {
    it('applies a native start position after metadata is available', async () => {
        const video = document.createElement('video');
        Object.defineProperty(video, 'readyState', { configurable: true, value: 0 });
        Object.defineProperty(video, 'currentTime', {
            configurable: true,
            value: 0,
            writable: true
        });
        vi.spyOn(video, 'load').mockImplementation(() => undefined);

        const attachment = await attachMedia(
            video,
            '/video.mp4',
            false,
            {},
            vi.fn(),
            { startPositionSeconds: 42 }
        );
        expect(video.currentTime).toBe(0);

        video.dispatchEvent(new Event('loadedmetadata'));
        expect(video.currentTime).toBe(42);

        attachment.destroy();
        expect(video.getAttribute('src')).toBeNull();
    });

    it('bounds HLS network recovery before surfacing a fatal error', async () => {
        const fatal = vi.fn();
        const video = document.createElement('video');
        const attachment = await attachMedia(
            video,
            '/stream.m3u8?ApiKey=one&API_KEY=two&ToKeN=three'
                + '&ACCESS_TOKEN=four&X-EMBY-TOKEN=five&quality=auto',
            true,
            {},
            fatal,
            { startPositionSeconds: 17 }
        );
        const hls = hlsState.instances[0]!;

        hls.emitFatal('networkError');
        hls.emitFatal('networkError');
        hls.emitFatal('networkError');

        expect(hls.config.startPosition).toBe(17);
        const loadedUrl = new URL(String(hls.loadSource.mock.calls[0]![0]));
        expect([ ...loadedUrl.searchParams.entries() ]).toEqual([[ 'quality', 'auto' ]]);
        expect(hls.startLoad).toHaveBeenCalledTimes(2);
        expect(fatal).toHaveBeenCalledOnce();
        expect(fatal).toHaveBeenCalledWith('network');
        expect(hls.destroy).toHaveBeenCalledOnce();

        attachment.destroy();
        expect(hls.destroy).toHaveBeenCalledOnce();
    });

    it('bounds media recovery and swaps audio codec only on the second attempt', async () => {
        const fatal = vi.fn();
        await attachMedia(
            document.createElement('video'),
            '/stream.m3u8',
            true,
            {},
            fatal
        );
        const hls = hlsState.instances[0]!;

        hls.emitFatal('mediaError');
        hls.emitFatal('mediaError');
        hls.emitFatal('mediaError');

        expect(hls.recoverMediaError).toHaveBeenCalledTimes(2);
        expect(hls.swapAudioCodec).toHaveBeenCalledOnce();
        expect(fatal).toHaveBeenCalledWith('codec');
        expect(hls.destroy).toHaveBeenCalledOnce();
    });

    it.each([
        [
            'Chromium',
            'Mozilla/5.0 AppleWebKit/537.36 Chrome/126.0.0.0 Safari/537.36'
        ],
        [
            'Firefox',
            'Mozilla/5.0 Gecko/20100101 Firefox/128.0'
        ]
    ])('uses hls.js on %s even when the media element claims native HLS support', async (
        _browser,
        userAgent
    ) => {
        vi.spyOn(window.navigator, 'userAgent', 'get').mockReturnValue(
            userAgent
        );
        const video = document.createElement('video');
        vi.spyOn(video, 'canPlayType').mockReturnValue('probably');

        await attachMedia(video, '/stream.m3u8', true, {}, vi.fn());

        expect(hlsState.instances).toHaveLength(1);
        expect(hlsState.instances[0]!.loadSource).toHaveBeenCalled();
    });

    it.each([
        [
            'Safari',
            'Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) '
                + 'AppleWebKit/605.1.15 Version/17.5 Safari/605.1.15'
        ],
        [
            'iOS',
            'Mozilla/5.0 (iPhone; CPU iPhone OS 17_5 like Mac OS X) '
                + 'AppleWebKit/605.1.15 CriOS/126.0 Mobile/15E148 Safari/604.1'
        ]
    ])('uses native HLS on %s when supported by the media element', async (
        _browser,
        userAgent
    ) => {
        vi.spyOn(window.navigator, 'userAgent', 'get').mockReturnValue(
            userAgent
        );
        const video = document.createElement('video');
        vi.spyOn(video, 'canPlayType').mockReturnValue('probably');
        vi.spyOn(video, 'load').mockImplementation(() => undefined);

        const attachment = await attachMedia(video, '/stream.m3u8', true, {}, vi.fn());

        expect(hlsState.instances).toHaveLength(0);
        expect(video.src).toContain('/stream.m3u8');
        attachment.destroy();
    });

    it('resets recovery budgets outside the configured time window', async () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2026-01-01T00:00:00Z'));
        const fatal = vi.fn();
        await attachMedia(
            document.createElement('video'),
            '/stream.m3u8',
            true,
            {},
            fatal
        );
        const hls = hlsState.instances[0]!;

        hls.emitFatal('networkError');
        hls.emitFatal('networkError');
        vi.advanceTimersByTime(30_001);
        hls.emitFatal('networkError');
        hls.emitFatal('networkError');

        expect(hls.startLoad).toHaveBeenCalledTimes(4);
        expect(fatal).not.toHaveBeenCalled();

        hls.emitFatal('networkError');
        expect(fatal).toHaveBeenCalledWith('network');
    });
});
