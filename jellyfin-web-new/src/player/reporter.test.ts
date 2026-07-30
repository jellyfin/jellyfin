import { afterEach, describe, expect, it, vi } from 'vitest';

import {
    PROGRESS_INTERVAL_MS,
    ProgressReporter,
    shouldReportProgress
} from './reporter';
import type { PlaybackHttpClient } from './types';

const createReporter = (
    requestMock: ReturnType<typeof vi.fn>,
    onSessionExpired?: () => void
) => {
    const client: PlaybackHttpClient = {
        request: requestMock as PlaybackHttpClient['request'],
        url: path => `https://example.test/${path}${path.includes('?') ? '&' : '?'}ApiKey=secret&X-Emby-Token=other`,
        authHeaders: () => ({ Authorization: 'MediaBrowser Token="secret"' }),
        mediaUrl: path => `https://example.test/${path}`
    };
    return new ProgressReporter({
        client,
        item: { Id: 'item-1' },
        profileId: 'profile-1',
        playSessionId: 'session-1',
        selection: {
            method: 'DirectPlay',
            source: { Id: 'source-1' },
            url: 'https://example.test/video.mp4',
            isHls: false
        },
        readState: () => ({
            currentTime: 12,
            duration: 120,
            paused: false,
            muted: false,
            volume: 0.5,
            playbackRate: 1
        }),
        onSessionExpired
    });
};

afterEach(() => {
    vi.useRealTimers();
    vi.unstubAllGlobals();
});

describe('progress cadence', () => {
    it('reports every ten seconds and always reports forced events', () => {
        expect(shouldReportProgress(1_000, 1_000 + PROGRESS_INTERVAL_MS - 1)).toBe(false);
        expect(shouldReportProgress(1_000, 1_000 + PROGRESS_INTERVAL_MS)).toBe(true);
        expect(shouldReportProgress(1_000, 1_001, true)).toBe(true);
    });

    it('starts once, reports on schedule, and stops once', async () => {
        vi.useFakeTimers();
        const request = vi.fn().mockResolvedValue({});
        const keepalive = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('fetch', keepalive);
        const reporter = createReporter(request);

        await Promise.all([ reporter.start(), reporter.start() ]);
        expect(request.mock.calls.map(([ path ]) => path)).toEqual([
            'Sessions/Playing',
            'Sessions/Playing/Progress',
            'CustomNetflix/v1/profiles/profile-1/progress'
        ]);

        await vi.advanceTimersByTimeAsync(PROGRESS_INTERVAL_MS);
        expect(request).toHaveBeenCalledTimes(5);

        reporter.stop();
        reporter.stop();
        expect(keepalive).toHaveBeenCalledTimes(2);
        expect(keepalive.mock.calls.every(([ url ]) =>
            !/secret|other/.test(String(url)))).toBe(true);

        await vi.advanceTimersByTimeAsync(PROGRESS_INTERVAL_MS);
        expect(request).toHaveBeenCalledTimes(5);
    });

    it('sends its final reports when the page is hidden by navigation', async () => {
        const request = vi.fn().mockResolvedValue({});
        const keepalive = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('fetch', keepalive);
        const reporter = createReporter(request);

        await reporter.start();
        window.dispatchEvent(new PageTransitionEvent('pagehide'));

        expect(keepalive).toHaveBeenCalledTimes(2);
    });

    it('does not install reporting after being stopped during startup', async () => {
        vi.useFakeTimers();
        let resolveStart!: (value: unknown) => void;
        const pendingStart = new Promise(resolve => {
            resolveStart = resolve;
        });
        const request = vi.fn((path: string) =>
            path === 'Sessions/Playing' ? pendingStart : Promise.resolve({}));
        const keepalive = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('fetch', keepalive);
        const reporter = createReporter(request);

        const startup = reporter.start();
        reporter.stop();
        resolveStart({});
        await startup;
        await vi.advanceTimersByTimeAsync(PROGRESS_INTERVAL_MS * 2);

        expect(request).toHaveBeenCalledTimes(1);
        expect(keepalive).toHaveBeenCalledTimes(2);
    });

    it('notifies once and does not retry when startup returns 401', async () => {
        const unauthorized = Object.assign(new Error('unauthorized'), { status: 401 });
        const request = vi.fn().mockRejectedValue(unauthorized);
        const onSessionExpired = vi.fn();
        const reporter = createReporter(request, onSessionExpired);

        await expect(reporter.start()).rejects.toBe(unauthorized);

        expect(request).toHaveBeenCalledTimes(1);
        expect(onSessionExpired).toHaveBeenCalledTimes(1);
    });

    it('retries transient startup failures without duplicating startup work', async () => {
        vi.useFakeTimers();
        const request = vi.fn()
            .mockRejectedValueOnce(Object.assign(new Error('unavailable'), { status: 503 }))
            .mockRejectedValueOnce(new TypeError('network'))
            .mockResolvedValue({});
        const keepalive = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('fetch', keepalive);
        const reporter = createReporter(request);

        const startup = reporter.start();
        expect(reporter.start()).toBe(startup);
        await vi.advanceTimersByTimeAsync(2_000);
        await startup;

        expect(request.mock.calls.filter(([ path ]) => path === 'Sessions/Playing'))
            .toHaveLength(3);
        reporter.stop();
    });

    it('bounds transient startup retries', async () => {
        vi.useFakeTimers();
        const unavailable = Object.assign(new Error('unavailable'), { status: 503 });
        const request = vi.fn().mockRejectedValue(unavailable);
        const reporter = createReporter(request);

        const startup = expect(reporter.start()).rejects.toBe(unavailable);
        await vi.advanceTimersByTimeAsync(2_000);
        await startup;

        expect(request).toHaveBeenCalledTimes(3);
    });

    it('cancels a pending startup retry when stopped', async () => {
        vi.useFakeTimers();
        const request = vi.fn()
            .mockRejectedValueOnce(Object.assign(new Error('unavailable'), { status: 503 }))
            .mockResolvedValue({});
        const keepalive = vi.fn().mockResolvedValue(undefined);
        vi.stubGlobal('fetch', keepalive);
        const reporter = createReporter(request);

        const startup = reporter.start();
        await vi.advanceTimersByTimeAsync(0);
        reporter.stop();
        await startup;
        await vi.advanceTimersByTimeAsync(2_000);

        expect(request).toHaveBeenCalledTimes(1);
        expect(keepalive).toHaveBeenCalledTimes(2);
    });
});
