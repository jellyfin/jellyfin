import { describe, expect, it } from 'vitest';

import {
    buildDeviceProfile,
    buildPlaybackRequest,
    chooseStream,
    chooseSubtitleStream,
    type BrowserEnvironment
} from './deviceProfile';
import type { PlaybackPreferences } from './types';

const environment: BrowserEnvironment = {
    userAgent: 'Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 Chrome/140.0.0.0 Safari/537.36 Edg/140.0.0.0',
    platform: 'Win32',
    maxTouchPoints: 0,
    hasMediaSource: true,
    hasWorker: true,
    hasCanvas: true,
    dynamicRangeHigh: false
};

const video = (
    supports: (mime: string) => boolean,
    options: { audioTracks?: boolean; textTracks?: boolean } = {}
) => ({
    canPlayType: (mime: string) => supports(mime.toLowerCase()) ? 'probably' : '',
    audioTracks: options.audioTracks ? {} : undefined,
    textTracks: options.textTracks === false ? undefined : {}
}) as unknown as HTMLVideoElement;

type Profile = ReturnType<typeof buildDeviceProfile>;
type ProfileEntry = Record<string, unknown>;
const entries = (profile: Profile, key: keyof Profile) =>
    profile[key] as ProfileEntry[];

describe('buildDeviceProfile', () => {
    it('advertises only detected Chromium codecs and both HLS containers', () => {
        const media = video(mime =>
            mime.includes('avc1.42e01e')
            || mime.includes('avc1.640029, mp4a.40.5')
            || mime.includes('hvc1.1.4.l120')
            || mime.includes('vp09')
            || mime.includes('video/webm; codecs="vp9"')
            || mime.includes('video/webm; codecs="vp9, opus"')
            || mime.includes('video/mp4; codecs="opus"')
            || mime.includes('av01.0.15m.08')
            || mime.includes('av01.0.15m.10')
        );
        const profile = buildDeviceProfile(media, environment);
        const direct = entries(profile, 'DirectPlayProfiles');
        const mp4 = direct.find(entry => entry.Container === 'mp4,m4v');
        const webm = direct.find(entry => entry.Container === 'webm');
        const mkv = direct.find(entry => entry.Container === 'mkv');

        expect(mp4?.VideoCodec).toBe('h264,hevc,vp9,av1');
        expect(mp4?.AudioCodec).toBe('aac,opus');
        expect(webm?.VideoCodec).toBe('vp9,av1');
        expect(mkv).toBeDefined();
        expect(entries(profile, 'TranscodingProfiles').map(entry => entry.Container))
            .toEqual([ 'mp4', 'ts' ]);
    });

    it('applies Safari/iOS codec and HLS constraints', () => {
        const media = video(mime =>
            mime.includes('avc1.42e01e')
            || mime.includes('hvc1.1.4.l120')
            || mime.includes('vp09')
            || mime.includes('video/webm; codecs="vp9"')
            || mime.includes('application/vnd.apple.mpegurl')
        );
        const profile = buildDeviceProfile(media, {
            ...environment,
            userAgent: 'Mozilla/5.0 (iPhone; CPU iPhone OS 16_4 like Mac OS X) Version/16.4 Mobile/15E148 Safari/604.1',
            platform: 'iPhone',
            maxTouchPoints: 5,
            hasMediaSource: false
        });
        const direct = entries(profile, 'DirectPlayProfiles');
        const mp4 = direct.find(entry => entry.Container === 'mp4,m4v');
        const hevcProfile = entries(profile, 'CodecProfiles')
            .find(entry => entry.Codec === 'hevc');
        const conditions = hevcProfile?.Conditions as ProfileEntry[];

        expect(mp4?.VideoCodec).toBe('h264,hevc');
        expect(direct.some(entry => entry.Container === 'webm')).toBe(false);
        expect(conditions).toContainEqual(expect.objectContaining({
            Property: 'VideoCodecTag',
            Value: 'hvc1|dvh1'
        }));
        expect(entries(profile, 'TranscodingProfiles')[0]).toEqual(
            expect.objectContaining({ MinSegments: '2' })
        );
    });

    it('avoids Firefox MKV and its known fMP4 regression at version 149', () => {
        const media = video(mime =>
            mime.includes('avc1.42e01e')
            || mime.includes('video/webm; codecs="vp9"')
            || mime.includes('video/webm; codecs="vp9, opus"')
            || mime.includes('video/x-matroska')
        );
        const profile = buildDeviceProfile(media, {
            ...environment,
            userAgent: 'Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:149.0) Gecko/20100101 Firefox/149.0'
        });

        expect(entries(profile, 'DirectPlayProfiles')
            .some(entry => entry.Container === 'mkv')).toBe(false);
        expect(entries(profile, 'TranscodingProfiles')[0]?.Container).toBe('ts');
    });

    it('does not claim MP4 or HLS when compatible audio is not detected', () => {
        const profile = buildDeviceProfile(
            video(mime => mime === 'video/mp4; codecs="avc1.42e01e"'),
            environment
        );

        expect(entries(profile, 'DirectPlayProfiles')).toEqual([]);
        expect(entries(profile, 'TranscodingProfiles')).toEqual([]);
    });

    it('keeps AV1 container capability checks independent', () => {
        const mp4 = buildDeviceProfile(video(mime =>
            mime.includes('av01.0.15m')
            || mime.includes('avc1.42e01e, mp4a.40.2')
        ), environment);
        const webm = buildDeviceProfile(video(mime =>
            mime.includes('video/webm; codecs="av01.0.15m')
            || mime.includes('video/webm; codecs="vp9, opus"')
        ), environment);

        expect(entries(mp4, 'DirectPlayProfiles')
            .find(entry => entry.Container === 'mp4,m4v')?.VideoCodec).toContain('av1');
        expect(entries(mp4, 'DirectPlayProfiles')
            .some(entry => entry.Container === 'webm')).toBe(false);
        expect(entries(webm, 'DirectPlayProfiles')
            .find(entry => entry.Container === 'webm')?.VideoCodec).toContain('av1');
    });

    it('advertises advanced external subtitles only with their browser primitives', () => {
        const media = video(mime => mime.includes('avc1.42e01e'));
        const basic = buildDeviceProfile(media, {
            ...environment,
            hasWorker: false,
            hasCanvas: false
        });
        const advanced = buildDeviceProfile(media, environment);

        expect(entries(basic, 'SubtitleProfiles').map(entry => entry.Format)).toEqual([ 'vtt' ]);
        expect(entries(advanced, 'SubtitleProfiles').map(entry => entry.Format))
            .toEqual([ 'vtt', 'ass', 'ssa', 'pgssub' ]);
    });
});

describe('playback preferences', () => {
    const preferences: PlaybackPreferences = {
        PreferDirectPlay: false,
        AllowContainerRemuxing: true,
        AllowVideoTranscoding: false,
        AllowAudioTranscoding: true,
        PreferHardwareTranscoding: true,
        MaxStreamingBitrate: 8_000_000,
        PreferredAudioLanguage: 'fr-FR',
        PreferredSubtitleLanguage: 'en',
        SubtitlesEnabled: false,
        AudioDescriptionEnabled: true,
        ClosedCaptionsEnabled: true,
        SkipCreditsEnabled: false
    };

    it('selects the preferred accessible stream and applies permissions', () => {
        const streams = [
            { Index: 0, Type: 'Audio', Language: 'en', IsDefault: true },
            { Index: 1, Type: 'Audio', Language: 'fr', Title: 'Audio Description' },
            { Index: 2, Type: 'Subtitle', Language: 'en', Title: 'CC' }
        ];

        expect(chooseStream(streams, 'Audio', 'fr-FR', true)).toBe(1);
        const request = buildPlaybackRequest('user', preferences, -5, streams, {
            mediaSourceId: 'source-1'
        });
        expect(request).toEqual(expect.objectContaining({
            StartTimeTicks: 0,
            MediaSourceId: 'source-1',
            AudioStreamIndex: 1,
            SubtitleStreamIndex: undefined,
            MaxStreamingBitrate: 8_000_000,
            EnableDirectPlay: false,
            EnableDirectStream: true,
            EnableTranscoding: true,
            AllowVideoStreamCopy: true,
            AllowAudioStreamCopy: true
        }));
    });

    it('keeps the server remux endpoint available without codec transcoding', () => {
        const request = buildPlaybackRequest('user', {
            ...preferences,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: false,
            AllowContainerRemuxing: true
        }, 0);

        expect(request.EnableTranscoding).toBe(true);
        expect(request.EnableDirectStream).toBe(true);
    });

    it('can force subtitles to server burn-in after renderer failure', () => {
        const request = buildPlaybackRequest('user', preferences, 0, undefined, {
            forceSubtitleBurnIn: true
        });
        const profile = request.DeviceProfile as Profile;

        expect(entries(profile, 'SubtitleProfiles').map(entry => entry.Format))
            .toEqual([]);
    });

    it('honors a subtitle selected explicitly when the profile default is off', () => {
        expect(chooseSubtitleStream(undefined, preferences, 4)).toBe(4);
        expect(chooseSubtitleStream(undefined, preferences, 4, true)).toBeUndefined();

        const request = buildPlaybackRequest(
            'user',
            preferences,
            0,
            undefined,
            { subtitleStreamIndex: 4 }
        );

        expect(request.SubtitleStreamIndex).toBe(4);
    });
});
