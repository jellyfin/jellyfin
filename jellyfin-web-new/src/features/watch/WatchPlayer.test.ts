import { describe, expect, it } from 'vitest';

import {
    formatBitrate,
    formatTime,
    playbackRestartPosition,
    playerShortcut,
    shouldRetryWithTranscode
} from './WatchPlayer';

describe('player controls', () => {
    it('formats short and long playback positions', () => {
        expect(formatTime(5.9)).toBe('0:05');
        expect(formatTime(3_725)).toBe('1:02:05');
        expect(formatTime(Number.NaN)).toBe('0:00');
    });

    it('maps the documented keyboard controls', () => {
        expect(playerShortcut(' ')).toBe('play');
        expect(playerShortcut('ArrowLeft')).toBe('seek-back');
        expect(playerShortcut('ArrowRight')).toBe('seek-forward');
        expect(playerShortcut('m')).toBe('mute');
        expect(playerShortcut('F')).toBe('fullscreen');
        expect(playerShortcut('Escape')).toBeUndefined();
    });

    it('formats technical bitrate and retries only decoder failures', () => {
        expect(formatBitrate(12_500_000)).toBe('13 Mb/s');
        expect(formatBitrate(null)).toBe('—');
        expect(playbackRestartPosition(42, 5)).toBe(42);
        expect(playbackRestartPosition(0, 5)).toBe(5);
        expect(shouldRetryWithTranscode(4, 'DirectPlay', false)).toBe(true);
        expect(shouldRetryWithTranscode(3, 'DirectStream', false)).toBe(true);
        expect(shouldRetryWithTranscode(2, 'DirectPlay', false)).toBe(false);
        expect(shouldRetryWithTranscode(4, 'Transcode', false)).toBe(false);
        expect(shouldRetryWithTranscode(4, 'DirectPlay', true)).toBe(false);
    });
});
