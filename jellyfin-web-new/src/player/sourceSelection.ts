import type {
    MediaSource,
    PlaybackInfoResponse,
    PlaybackPreferences,
    PlaybackSelection
} from './types';

const firstContainer = (source: MediaSource) => {
    const container = source.Container?.split(',')[0]?.trim().toLowerCase();
    return container && /^[a-z0-9]+$/.test(container) ? container : null;
};

const transcodeReasons = (url: string) => {
    try {
        const parameters = new URL(url, 'https://jellyfin.invalid').searchParams;
        const reasons = [ ...parameters.entries() ]
            .find(([ key ]) => key.toLowerCase() === 'transcodereasons')?.[1];
        return reasons?.split(',').map(reason => reason.trim()).filter(Boolean) ?? [];
    } catch {
        return [];
    }
};

const analyzeReasons = (url: string) => {
    const reasons = transcodeReasons(url);
    const audio = reasons.some(reason =>
        reason.startsWith('Audio') || reason === 'SecondaryAudioNotSupported');
    const remux = reasons.some(reason =>
        reason === 'ContainerNotSupported' || reason === 'VideoCodecTagNotSupported');
    const video = reasons.some(reason =>
        (reason.startsWith('Video') && reason !== 'VideoCodecTagNotSupported')
        || reason.startsWith('Subtitle')
        || reason === 'ContainerBitrateExceedsLimit');
    const unknown = reasons.some(reason =>
        !reason.startsWith('Audio')
        && !reason.startsWith('Video')
        && !reason.startsWith('Subtitle')
        && reason !== 'SecondaryAudioNotSupported'
        && reason !== 'ContainerNotSupported'
        && reason !== 'ContainerBitrateExceedsLimit');
    return { audio, reasons, remux, unknown, video };
};

const isHls = (source: MediaSource, url: string) =>
    source.TranscodingSubProtocol?.toLowerCase() === 'hls'
    || source.TranscodingContainer?.toLowerCase() === 'hls'
    || /\.m3u8(?:$|\?)/i.test(url)
    || [ 'hls', 'm3u8' ].includes(firstContainer(source) ?? '');

const isStaticHls = (source: MediaSource, url: string) =>
    /\.m3u8(?:$|\?)/i.test(url)
    || [ 'hls', 'm3u8' ].includes(firstContainer(source) ?? '');

const canUseTranscodingUrl = (url: string, preferences: PlaybackPreferences) => {
    if (!preferences.AllowVideoTranscoding && !preferences.AllowAudioTranscoding) return false;
    if (preferences.AllowVideoTranscoding && preferences.AllowAudioTranscoding) return true;

    const reasons = analyzeReasons(url);
    // Without complete reasons, accepting a one-sided permission could silently
    // transcode the forbidden stream.
    if (!reasons.reasons.length || reasons.unknown) return false;
    if (reasons.audio && !preferences.AllowAudioTranscoding) return false;
    if (reasons.video && !preferences.AllowVideoTranscoding) return false;
    if (reasons.remux && !reasons.audio && !reasons.video) return false;
    return reasons.audio || reasons.video;
};

const canUseAsDirectStream = (url: string, preferences: PlaybackPreferences) => {
    if (!preferences.AllowContainerRemuxing) return false;
    const reasons = analyzeReasons(url);
    return reasons.reasons.length > 0
        && !reasons.video
        && !reasons.unknown
        && (reasons.remux || reasons.audio)
        && (!reasons.audio || preferences.AllowAudioTranscoding);
};

const staticSelection = (
    method: 'DirectPlay' | 'DirectStream',
    source: MediaSource,
    response: PlaybackInfoResponse,
    itemId: string,
    mediaUrl: (path: string, query?: Record<string, boolean | number | string | null | undefined>) => string,
    selectedStreams: {
        audioStreamIndex?: number | undefined;
        subtitleStreamIndex?: number | undefined;
    }
): PlaybackSelection | null => {
    const container = firstContainer(source);
    if (!source.Id || !container) return null;
    const url = mediaUrl(`Videos/${encodeURIComponent(itemId)}/stream.${container}`, {
        static: true,
        mediaSourceId: source.Id,
        playSessionId: response.PlaySessionId
    });
    return {
        method,
        source,
        url,
        isHls: isStaticHls(source, url),
        ...selectedStreams
    };
};

export function selectPlaybackSource(
    response: PlaybackInfoResponse,
    itemId: string,
    preferences: PlaybackPreferences,
    mediaUrl: (path: string, query?: Record<string, boolean | number | string | null | undefined>) => string,
    selectedStreams: {
        audioStreamIndex?: number | undefined;
        subtitleStreamIndex?: number | undefined;
    } = {}
): PlaybackSelection {
    if (response.ErrorCode) {
        throw new Error(`PlaybackInfo:${response.ErrorCode}`);
    }

    const sources = response.MediaSources ?? [];
    if (preferences.PreferDirectPlay) {
        for (const source of sources) {
            if (!source.SupportsDirectPlay) continue;
            const selection = staticSelection(
                'DirectPlay',
                source,
                response,
                itemId,
                mediaUrl,
                selectedStreams
            );
            if (selection) return selection;
        }
    }

    const directStream = sources.find(source =>
        (source.SupportsDirectStream || source.SupportsTranscoding)
        && source.TranscodingUrl
        && canUseAsDirectStream(source.TranscodingUrl, preferences));
    if (directStream?.TranscodingUrl) {
        return {
            method: 'DirectStream',
            source: directStream,
            url: mediaUrl(directStream.TranscodingUrl),
            isHls: isHls(directStream, directStream.TranscodingUrl),
            ...selectedStreams
        };
    }

    const transcode = sources.find(source =>
        source.SupportsTranscoding
        && source.TranscodingUrl
        && canUseTranscodingUrl(source.TranscodingUrl, preferences));
    if (transcode?.TranscodingUrl) {
        return {
            method: 'Transcode',
            source: transcode,
            url: mediaUrl(transcode.TranscodingUrl),
            isHls: isHls(transcode, transcode.TranscodingUrl),
            ...selectedStreams
        };
    }

    throw new Error('PlaybackInfo:TranscodingUnavailable');
}
