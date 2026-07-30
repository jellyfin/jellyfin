import type { MediaStream, PlaybackPreferences, PlaybackRequest } from './types';

type BrowserFamily = 'chromium' | 'firefox' | 'safari' | 'unknown';

export interface BrowserEnvironment {
    userAgent: string;
    platform: string;
    maxTouchPoints: number;
    hasMediaSource: boolean;
    hasWorker: boolean;
    hasCanvas: boolean;
    dynamicRangeHigh: boolean;
}

const defaultEnvironment = (): BrowserEnvironment => ({
    userAgent: navigator.userAgent,
    platform: navigator.platform,
    maxTouchPoints: navigator.maxTouchPoints ?? 0,
    hasMediaSource: typeof MediaSource !== 'undefined',
    hasWorker: typeof Worker !== 'undefined',
    hasCanvas: typeof HTMLCanvasElement !== 'undefined',
    dynamicRangeHigh: typeof matchMedia === 'function'
        && matchMedia('(dynamic-range: high)').matches
});

const canPlay = (element: HTMLVideoElement, ...mimeTypes: string[]) =>
    mimeTypes.some(mime => {
        try {
            return element.canPlayType(mime) === 'maybe'
                || element.canPlayType(mime) === 'probably';
        } catch {
            return false;
        }
    });

const unique = (values: Array<false | string>) => [ ...new Set(values.filter(Boolean)) ] as string[];

const browserDetails = (environment: BrowserEnvironment) => {
    const { userAgent } = environment;
    const ios = /iP(?:hone|ad|od)/i.test(userAgent)
        || environment.platform === 'MacIntel' && environment.maxTouchPoints > 1;
    const firefoxMatch = userAgent.match(/Firefox\/(\d+)/i);
    const chromiumMatch = userAgent.match(/(?:Chrome|Chromium|CriOS)\/(\d+)/i);
    const edgeMatch = userAgent.match(/Edg(?:A|iOS)?\/(\d+)/i);
    const safariMatch = userAgent.match(/Version\/(\d+)/i);
    const family: BrowserFamily = ios
        ? 'safari'
        : firefoxMatch
            ? 'firefox'
            : chromiumMatch || edgeMatch
                ? 'chromium'
                : /Safari\//i.test(userAgent)
                    ? 'safari'
                    : 'unknown';
    const version = Number(
        firefoxMatch?.[1]
        ?? edgeMatch?.[1]
        ?? chromiumMatch?.[1]
        ?? safariMatch?.[1]
        ?? 0
    );

    return {
        family,
        version,
        ios,
        mac: /Mac/i.test(environment.platform),
        mobile: ios || /Android|Mobile/i.test(userAgent),
        edge: Boolean(edgeMatch),
        windows: /Windows/i.test(userAgent)
    };
};

const normalizeLanguage = (language?: string | null) =>
    language?.trim().toLowerCase().split('-')[0] ?? '';

const isDescriptiveAudio = (stream: MediaStream) =>
    /audio description|audiodescription|descriptive|description audio|ad\b/i
        .test(`${stream.Title ?? ''} ${stream.DisplayTitle ?? ''}`);

const isClosedCaption = (stream: MediaStream) =>
    /\bcc\b|closed captions?|eia.?60[89]|cea.?60[89]/i
        .test(`${stream.Codec ?? ''} ${stream.Title ?? ''} ${stream.DisplayTitle ?? ''}`);

export function chooseStream(
    streams: MediaStream[] | null | undefined,
    type: 'Audio' | 'Subtitle',
    language?: string | null,
    preferAccessibility = false
) {
    const candidates = (streams ?? []).filter(stream => stream.Type === type && stream.Index != null);
    const preferredLanguage = normalizeLanguage(language);

    return candidates
        .map((stream, index) => ({
            index,
            stream,
            score:
                (preferredLanguage && normalizeLanguage(stream.Language) === preferredLanguage ? 8 : 0)
                + (preferAccessibility && (type === 'Audio'
                    ? isDescriptiveAudio(stream)
                    : isClosedCaption(stream)) ? 4 : 0)
                + (stream.IsDefault ? 2 : 0)
                + (stream.IsForced ? 1 : 0)
        }))
        .sort((left, right) => right.score - left.score || left.index - right.index)[0]
        ?.stream.Index ?? undefined;
}

export function chooseSubtitleStream(
    streams: MediaStream[] | null | undefined,
    preferences: PlaybackPreferences,
    selected?: number,
    disabled = false
) {
    if (disabled) return undefined;
    return selected ?? (preferences.SubtitlesEnabled
        ? chooseStream(
            streams,
            'Subtitle',
            preferences.PreferredSubtitleLanguage,
            preferences.ClosedCaptionsEnabled
        )
        : undefined);
}

export function buildDeviceProfile(
    video = document.createElement('video'),
    environmentOverrides: Partial<BrowserEnvironment> = {}
) {
    const environment = { ...defaultEnvironment(), ...environmentOverrides };
    const browser = browserDetails(environment);
    const nativeHls = canPlay(
        video,
        'application/vnd.apple.mpegurl',
        'application/x-mpegurl'
    );
    const supportsHls = nativeHls || environment.hasMediaSource;

    const h264 = canPlay(video, 'video/mp4; codecs="avc1.42E01E"');
    const hevcMain = canPlay(
        video,
        'video/mp4; codecs="hvc1.1.4.L120"',
        'video/mp4; codecs="hev1.1.4.L120"'
    );
    const hevcMain10 = canPlay(
        video,
        'video/mp4; codecs="hvc1.2.4.L153"',
        'video/mp4; codecs="hev1.2.4.L153"'
    );
    const vp8 = canPlay(video, 'video/webm; codecs="vp8"');
    const vp9Webm = canPlay(video, 'video/webm; codecs="vp9"');
    const vp9Mp4 = canPlay(video, 'video/mp4; codecs="vp09.00.10.08"')
        && !browser.ios
        && !(browser.family === 'firefox' && browser.mac);
    const av1 = canPlay(video, 'video/mp4; codecs="av01.0.15M.08"')
        && canPlay(video, 'video/mp4; codecs="av01.0.15M.10"');
    const av1Webm = canPlay(video, 'video/webm; codecs="av01.0.15M.08"')
        && canPlay(video, 'video/webm; codecs="av01.0.15M.10"');

    const aac = canPlay(video, 'video/mp4; codecs="avc1.42E01E, mp4a.40.2"');
    const heAac = canPlay(video, 'video/mp4; codecs="avc1.640029, mp4a.40.5"');
    const mp3 = canPlay(
        video,
        'video/mp4; codecs="avc1.640029, mp3"',
        'video/mp4; codecs="avc1.640029, mp4a.6B"'
    );
    const mp2 = mp3 && !browser.mobile
        && (browser.family === 'chromium'
            || browser.family === 'firefox' && browser.version >= 83);
    const ac3 = canPlay(video, 'audio/mp4; codecs="ac-3"');
    const eac3 = canPlay(video, 'audio/mp4; codecs="ec-3"');
    const opusMp4 = canPlay(video, 'video/mp4; codecs="opus"')
        || browser.family === 'safari'
            && browser.version >= 17
            && canPlay(video, 'audio/x-caf; codecs="opus"');
    const flacMp4 = canPlay(video, 'video/mp4; codecs="flac"');
    const alacMp4 = canPlay(video, 'video/mp4; codecs="alac"');
    const dts = canPlay(video, 'video/mp4; codecs="dts-"', 'video/mp4; codecs="dts+"');
    const webmOpus = canPlay(video, 'video/webm; codecs="vp9, opus"');
    const webmVorbis = canPlay(video, 'video/webm; codecs="vp8, vorbis"');

    const mp4VideoCodecs = unique([
        h264 && 'h264',
        (hevcMain || hevcMain10) && 'hevc',
        vp9Mp4 && 'vp9',
        av1 && 'av1'
    ]);
    const mp4AudioCodecs = unique([
        aac && 'aac',
        mp3 && 'mp3',
        mp2 && 'mp2',
        ac3 && 'ac3',
        eac3 && 'eac3',
        opusMp4 && 'opus',
        flacMp4 && 'flac',
        alacMp4 && 'alac',
        dts && 'dca',
        dts && 'dts'
    ]);
    const webmVideoCodecs = unique([
        vp8 && 'vp8',
        vp9Webm && 'vp9',
        av1Webm && 'av1'
    ]);
    const webmAudioCodecs = unique([
        webmVorbis && 'vorbis',
        webmOpus && 'opus'
    ]);

    const directPlayProfiles: Record<string, unknown>[] = [];
    if (mp4VideoCodecs.length && mp4AudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'mp4,m4v',
            Type: 'Video',
            VideoCodec: mp4VideoCodecs.join(','),
            AudioCodec: mp4AudioCodecs.join(',')
        });
    }
    const safariWebmReliable = browser.family !== 'safari'
        || !browser.mobile && browser.version > 0 && browser.version < 17;
    if (safariWebmReliable && webmVideoCodecs.length && webmAudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'webm',
            Type: 'Video',
            VideoCodec: webmVideoCodecs.join(','),
            AudioCodec: webmAudioCodecs.join(',')
        });
    }
    const mkv = browser.family !== 'firefox'
        && (canPlay(video, 'video/x-matroska', 'video/mkv')
            || browser.edge && browser.windows);
    if (mkv && mp4VideoCodecs.length && mp4AudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'mkv',
            Type: 'Video',
            VideoCodec: mp4VideoCodecs.join(','),
            AudioCodec: mp4AudioCodecs.join(',')
        });
    }
    if ((browser.family === 'safari' || browser.family === 'chromium')
        && h264 && mp4AudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'mov',
            Type: 'Video',
            VideoCodec: 'h264',
            AudioCodec: mp4AudioCodecs.join(',')
        });
    }

    const hlsFmp4VideoCodecs = unique([
        h264 && 'h264',
        (hevcMain || hevcMain10)
            && (browser.family === 'safari'
                || browser.family === 'chromium' && (!browser.mobile || browser.version >= 105)
                || browser.family === 'firefox' && browser.version >= 134)
            && 'hevc',
        (vp9Mp4 || vp9Webm) && 'vp9',
        av1 && (!browser.mobile || browser.family === 'safari') && 'av1'
    ]);
    const hlsMp3 = canPlay(
        video,
        'application/vnd.apple.mpegurl; codecs="avc1.42E01E, mp4a.40.34"',
        'application/x-mpegurl; codecs="avc1.42E01E, mp3"'
    );
    const hlsFmp4AudioCodecs = unique([
        aac && 'aac',
        hlsMp3 && 'mp3',
        mp2 && 'mp2',
        opusMp4 && 'opus',
        flacMp4 && 'flac',
        alacMp4 && 'alac',
        ac3 && 'ac3',
        eac3 && 'eac3'
    ]);
    const hlsAc3 = canPlay(
        video,
        'application/vnd.apple.mpegurl; codecs="avc1.42E01E, ac-3"',
        'application/x-mpegurl; codecs="avc1.42E01E, ac-3"'
    );
    const hlsTsVideoCodecs = h264 ? [ 'h264' ] : [];
    const hlsTsAudioCodecs = unique([
        aac && 'aac',
        (browser.family === 'safari' || hlsMp3) && 'mp3',
        mp2 && 'mp2',
        hlsAc3 && 'ac3',
        hlsAc3 && eac3 && 'eac3'
    ]);
    const enableFmp4Hls = !(browser.family === 'firefox' && browser.version === 149);
    if (supportsHls && enableFmp4Hls
        && hlsFmp4VideoCodecs.length && hlsFmp4AudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'hls',
            Type: 'Video',
            VideoCodec: hlsFmp4VideoCodecs.join(','),
            AudioCodec: hlsFmp4AudioCodecs.join(',')
        });
    }
    if (supportsHls && hlsTsVideoCodecs.length && hlsTsAudioCodecs.length) {
        directPlayProfiles.push({
            Container: 'hls',
            Type: 'Video',
            VideoCodec: hlsTsVideoCodecs.join(','),
            AudioCodec: hlsTsAudioCodecs.join(',')
        });
    }

    const transcodingProfiles: Record<string, unknown>[] = [];
    const transcodingBase = {
        Type: 'Video',
        Context: 'Streaming',
        Protocol: 'hls',
        MaxAudioChannels: '6',
        MinSegments: browser.family === 'safari' ? '2' : '1',
        BreakOnNonKeyFrames: browser.family === 'safari' || !nativeHls
    };
    if (supportsHls && enableFmp4Hls
        && hlsFmp4VideoCodecs.length && hlsFmp4AudioCodecs.length) {
        transcodingProfiles.push({
            ...transcodingBase,
            Container: 'mp4',
            VideoCodec: hlsFmp4VideoCodecs.join(','),
            AudioCodec: hlsFmp4AudioCodecs.join(',')
        });
    }
    if (supportsHls && hlsTsVideoCodecs.length && hlsTsAudioCodecs.length) {
        transcodingProfiles.push({
            ...transcodingBase,
            Container: 'ts',
            VideoCodec: hlsTsVideoCodecs.join(','),
            AudioCodec: hlsTsAudioCodecs.join(',')
        });
    }

    const supportsHdr = environment.dynamicRangeHigh
        || browser.family === 'safari'
        || browser.family === 'chromium' && !browser.mobile
        || browser.family === 'firefox' && browser.mac && browser.version >= 100;
    const hdrRanges = supportsHdr ? '|HDR10|HDR10Plus|HLG' : '';
    const codecProfiles: Record<string, unknown>[] = [];
    const secondaryAudioUnsupported =
        !(video as HTMLVideoElement & { audioTracks?: unknown }).audioTracks;
    if (aac && !heAac) {
        const conditions: Record<string, unknown>[] = [];
        conditions.push({
            Condition: 'NotEquals',
            Property: 'AudioProfile',
            Value: 'HE-AAC'
        });
        codecProfiles.push({ Type: 'VideoAudio', Codec: 'aac', Conditions: conditions });
    }
    if (secondaryAudioUnsupported) {
        codecProfiles.push({
            Type: 'VideoAudio',
            Conditions: [{
                Condition: 'Equals',
                Property: 'IsSecondaryAudio',
                Value: 'false',
                IsRequired: false
            }]
        });
    }

    const h264Level = canPlay(video, 'video/mp4; codecs="avc1.640834"')
        ? '52'
        : canPlay(video, 'video/mp4; codecs="avc1.640833"')
            ? '51'
            : '42';
    const h264Profiles = canPlay(video, 'video/mp4; codecs="avc1.6e0033"')
        && browser.family !== 'safari' && !browser.mobile
        ? 'high|main|baseline|constrained baseline|high 10'
        : 'high|main|baseline|constrained baseline';
    if (h264) {
        codecProfiles.push({
            Type: 'Video',
            Codec: 'h264',
            Conditions: [
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoProfile',
                    Value: h264Profiles,
                    IsRequired: false
                },
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoRangeType',
                    Value: 'SDR',
                    IsRequired: false
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'VideoLevel',
                    Value: h264Level,
                    IsRequired: false
                },
                {
                    Condition: 'NotEquals',
                    Property: 'IsInterlaced',
                    Value: 'true',
                    IsRequired: false
                }
            ]
        });
    }
    if (hevcMain || hevcMain10) {
        const dolbyVision = browser.family === 'safari' && canPlay(
            video,
            'video/mp4; codecs="dvh1.05.06"',
            'video/mp4; codecs="dvh1.08.06"'
        );
        const conditions: Record<string, unknown>[] = [
            {
                Condition: 'EqualsAny',
                Property: 'VideoProfile',
                Value: hevcMain10 ? 'main|main 10' : 'main',
                IsRequired: false
            },
            {
                Condition: 'EqualsAny',
                Property: 'VideoRangeType',
                Value: dolbyVision
                    ? `SDR${hdrRanges}|DOVI|DOVIWithHDR10|DOVIWithHLG|DOVIWithSDR`
                    : `SDR${hdrRanges}`,
                IsRequired: false
            },
            {
                Condition: 'LessThanEqual',
                Property: 'VideoLevel',
                Value: hevcMain10 ? '153' : '120',
                IsRequired: false
            },
            {
                Condition: 'NotEquals',
                Property: 'IsInterlaced',
                Value: 'true',
                IsRequired: false
            }
        ];
        if (browser.family === 'safari') {
            conditions.push(
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoCodecTag',
                    Value: 'hvc1|dvh1',
                    IsRequired: true
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'VideoFramerate',
                    Value: '60',
                    IsRequired: true
                }
            );
        }
        codecProfiles.push({ Type: 'Video', Codec: 'hevc', Conditions: conditions });
    }
    if (vp9Mp4 || vp9Webm) {
        codecProfiles.push({
            Type: 'Video',
            Codec: 'vp9',
            Conditions: [{
                Condition: 'EqualsAny',
                Property: 'VideoRangeType',
                Value: `SDR${hdrRanges}`,
                IsRequired: false
            }]
        });
    }
    if (av1) {
        codecProfiles.push({
            Type: 'Video',
            Codec: 'av1',
            Conditions: [
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoProfile',
                    Value: 'main',
                    IsRequired: false
                },
                {
                    Condition: 'EqualsAny',
                    Property: 'VideoRangeType',
                    Value: `SDR${hdrRanges}`,
                    IsRequired: false
                },
                {
                    Condition: 'LessThanEqual',
                    Property: 'VideoLevel',
                    Value: '15',
                    IsRequired: false
                }
            ]
        });
    }

    const subtitleProfiles: Record<string, unknown>[] = [];
    if (video.textTracks != null) {
        subtitleProfiles.push({ Format: 'vtt', Method: 'External' });
    }
    if (environment.hasWorker && environment.hasCanvas) {
        subtitleProfiles.push(
            { Format: 'ass', Method: 'External' },
            { Format: 'ssa', Method: 'External' },
            { Format: 'pgssub', Method: 'External' }
        );
    }

    return {
        Name: 'Jellyfin Web New',
        MaxStreamingBitrate: 120_000_000,
        MaxStaticBitrate: 100_000_000,
        DirectPlayProfiles: directPlayProfiles,
        TranscodingProfiles: transcodingProfiles,
        ContainerProfiles: [],
        CodecProfiles: codecProfiles,
        SubtitleProfiles: subtitleProfiles,
        ResponseProfiles: [{
            Type: 'Video',
            Container: 'm4v',
            MimeType: 'video/mp4'
        }]
    };
}

export function buildPlaybackRequest(
    userId: string,
    preferences: PlaybackPreferences,
    startTimeTicks: number,
    streams?: MediaStream[] | null,
    overrides: {
        audioStreamIndex?: number | undefined;
        forceSubtitleBurnIn?: boolean | undefined;
        mediaSourceId?: string | undefined;
        subtitleStreamIndex?: number | undefined;
    } = {}
): PlaybackRequest {
    const audioStreamIndex = overrides.audioStreamIndex ?? chooseStream(
        streams,
        'Audio',
        preferences.PreferredAudioLanguage,
        preferences.AudioDescriptionEnabled
    );
    const subtitleStreamIndex = chooseSubtitleStream(
        streams,
        preferences,
        overrides.subtitleStreamIndex
    );
    // Jellyfin exposes browser remuxing through its transcoding URL even when
    // neither elementary stream is re-encoded.
    const canTranscode = preferences.AllowContainerRemuxing
        || preferences.AllowVideoTranscoding
        || preferences.AllowAudioTranscoding;

    const deviceProfile = buildDeviceProfile();
    if (overrides.forceSubtitleBurnIn) {
        deviceProfile.SubtitleProfiles = [];
    }

    return {
        UserId: userId,
        MediaSourceId: overrides.mediaSourceId,
        MaxStreamingBitrate: preferences.MaxStreamingBitrate,
        StartTimeTicks: Math.max(0, Math.round(startTimeTicks)),
        AudioStreamIndex: audioStreamIndex,
        SubtitleStreamIndex: subtitleStreamIndex,
        DeviceProfile: deviceProfile,
        EnableDirectPlay: preferences.PreferDirectPlay,
        EnableDirectStream: preferences.AllowContainerRemuxing,
        EnableTranscoding: canTranscode,
        AllowVideoStreamCopy: preferences.AllowContainerRemuxing,
        AllowAudioStreamCopy: preferences.AllowContainerRemuxing,
        AutoOpenLiveStream: false
    };
}
