import { describe, expect, it } from 'vitest';

import { selectPlaybackSource } from './sourceSelection';
import type { PlaybackPreferences } from './types';

const preferences: PlaybackPreferences = {
    PreferDirectPlay: true,
    AllowContainerRemuxing: true,
    AllowVideoTranscoding: true,
    AllowAudioTranscoding: true,
    PreferHardwareTranscoding: true,
    SubtitlesEnabled: false,
    AudioDescriptionEnabled: false,
    ClosedCaptionsEnabled: false,
    SkipCreditsEnabled: false
};
const mediaUrl = (
    path: string,
    query: Record<string, boolean | number | string | null | undefined> = {}
) => {
    const url = new URL(path.replace(/^\//, ''), 'https://example.test/');
    Object.entries(query).forEach(([ key, value ]) => {
        if (value !== null && value !== undefined) url.searchParams.set(key, String(value));
    });
    return url.toString();
};

describe('selectPlaybackSource', () => {
    it('chooses direct play before remux and transcode', () => {
        const selection = selectPlaybackSource({
            PlaySessionId: 'play-1',
            MediaSources: [
                {
                    Id: 'source-1',
                    Container: 'mp4',
                    SupportsDirectPlay: true,
                    SupportsDirectStream: true,
                    SupportsTranscoding: true,
                    TranscodingUrl: '/Videos/item/master.m3u8'
                }
            ]
        }, 'item', preferences, mediaUrl);

        expect(selection.method).toBe('DirectPlay');
        expect(selection.url).toContain('stream.mp4');
        expect(selection.url).toContain('static=true');
    });

    it('falls back to remux before transcoding', () => {
        const selection = selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                Container: 'mp4',
                SupportsDirectPlay: false,
                SupportsDirectStream: true,
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=ContainerNotSupported',
                TranscodingSubProtocol: 'Hls'
            }]
        }, 'item', preferences, mediaUrl);

        expect(selection.method).toBe('DirectStream');
        expect(selection.url).toContain('master.m3u8');
        expect(selection.isHls).toBe(true);
    });

    it('uses the server remux URL for container-only incompatibility', () => {
        const selection = selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                Container: 'mkv',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?transcodeReasons=ContainerNotSupported',
                TranscodingSubProtocol: 'Hls'
            }]
        }, 'item', preferences, mediaUrl);

        expect(selection.method).toBe('DirectStream');
        expect(selection.isHls).toBe(true);
    });

    it('allows a container-only remux when audio and video transcoding are forbidden', () => {
        const selection = selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=ContainerNotSupported',
                TranscodingSubProtocol: 'hls'
            }]
        }, 'item', {
            ...preferences,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: false
        }, mediaUrl);

        expect(selection.method).toBe('DirectStream');
    });

    it('treats an unsupported video codec tag as remux, not video transcoding', () => {
        const selection = selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=VideoCodecTagNotSupported',
                TranscodingSubProtocol: 'hls'
            }]
        }, 'item', {
            ...preferences,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: false
        }, mediaUrl);

        expect(selection.method).toBe('DirectStream');
    });

    it('blocks remux-only playback when container remuxing is forbidden', () => {
        expect(() => selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=VideoCodecTagNotSupported'
            }]
        }, 'item', {
            ...preferences,
            AllowContainerRemuxing: false,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: false
        }, mediaUrl)).toThrow('TranscodingUnavailable');
    });

    it('rejects a forbidden audio transcode', () => {
        expect(() => selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=AudioCodecNotSupported'
            }]
        }, 'item', {
            ...preferences,
            AllowContainerRemuxing: false,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: true
        }, mediaUrl)).toThrow('TranscodingUnavailable');
    });

    it('allows audio-only direct streaming when video transcoding is disabled', () => {
        const selection = selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=ContainerNotSupported%2CAudioCodecNotSupported',
                TranscodingSubProtocol: 'hls'
            }]
        }, 'item', {
            ...preferences,
            AllowVideoTranscoding: false,
            AllowAudioTranscoding: true
        }, mediaUrl);

        expect(selection.method).toBe('DirectStream');
        expect(selection.isHls).toBe(true);
    });

    it('fails closed when one-sided permissions meet ambiguous reasons', () => {
        expect(() => selectPlaybackSource({
            MediaSources: [{
                Id: 'source-1',
                SupportsTranscoding: true,
                TranscodingUrl: '/Videos/item/master.m3u8'
            }]
        }, 'item', {
            ...preferences,
            AllowAudioTranscoding: false,
            AllowVideoTranscoding: true
        }, mediaUrl)).toThrow('TranscodingUnavailable');
    });

    it('skips a malformed direct container and uses a safe transcode', () => {
        const selection = selectPlaybackSource({
            MediaSources: [
                {
                    Id: 'bad',
                    Container: '../mp4',
                    SupportsDirectPlay: true
                },
                {
                    Id: 'safe',
                    SupportsTranscoding: true,
                    TranscodingUrl: '/Videos/item/master.m3u8?TranscodeReasons=VideoCodecNotSupported'
                }
            ]
        }, 'item', preferences, mediaUrl);

        expect(selection.method).toBe('Transcode');
        expect(selection.source.Id).toBe('safe');
    });
});
