import {
    useCallback,
    useEffect,
    useMemo,
    useRef,
    useState
} from 'react';

import {
    confirmStillWatching,
    getAutoplayAction,
    type AutoplayAction,
    type AutoplayDecision
} from '../../player/autoplay';
import {
    buildPlaybackRequest,
    chooseStream,
    chooseSubtitleStream
} from '../../player/deviceProfile';
import { attachMedia, type AttachedMedia } from '../../player/hls';
import { ProgressReporter } from '../../player/reporter';
import { findActiveSegment, loadMediaSegments } from '../../player/segments';
import { selectPlaybackSource } from '../../player/sourceSelection';
import { playerStore, usePlayerState } from '../../player/store';
import { attachSubtitle, type AttachedSubtitle } from '../../player/subtitles';
import type {
    MediaSource,
    MediaStream,
    PlaybackErrorCode,
    PlaybackHttpClient,
    PlaybackInfoResponse,
    PlaybackMethod,
    PlaybackPreferences,
    PlayerItem,
    ProfilePlaybackSettings
} from '../../player/types';

import styles from './WatchPlayer.module.css';

const QUALITY_OPTIONS = [
    { label: 'Auto', value: 0 },
    { label: '40 Mb/s', value: 40_000_000 },
    { label: '12 Mb/s', value: 12_000_000 },
    { label: '5 Mb/s', value: 5_000_000 },
    { label: '2.5 Mb/s', value: 2_500_000 }
];
const EMPTY_STREAMS: MediaStream[] = [];

const copy = {
    en: {
        back: 'Back',
        play: 'Play',
        pause: 'Pause',
        seek: 'Playback position',
        volume: 'Volume',
        mute: 'Mute',
        unmute: 'Unmute',
        speed: 'Speed',
        quality: 'Quality',
        audio: 'Audio',
        subtitles: 'Subtitles',
        off: 'Off',
        auto: 'Automatic',
        settings: 'Settings',
        closeSettings: 'Close settings',
        technical: 'Technical details',
        method: 'Playback mode',
        transport: 'Transport',
        container: 'Container',
        videoCodec: 'Video codec',
        audioCodec: 'Audio codec',
        source: 'Source',
        sourceBitrate: 'Source bitrate',
        bitrate: 'Maximum bitrate',
        shortcuts: 'Shortcuts: space play, ←/→ seek, M sound, F full screen.',
        controls: 'Playback controls',
        loading: 'Preparing playback…',
        fullscreen: 'Full screen',
        exitFullscreen: 'Exit full screen',
        pip: 'Picture in Picture',
        exitPip: 'Exit Picture in Picture',
        skip: 'Skip',
        intro: 'Skip intro',
        recap: 'Skip recap',
        credits: 'Skip credits',
        stillWatching: 'Are you still watching?',
        continue: 'Continue',
        stop: 'Stop',
        nextIn: 'Next episode in {seconds}s',
        errors: {
            codec: 'This codec cannot be played by this browser.',
            network: 'The network connection was interrupted.',
            'session-expired': 'Your session has expired. Sign in again.',
            'transcoding-unavailable': 'The server cannot provide a compatible stream.',
            unknown: 'Playback could not start.'
        }
    },
    fr: {
        back: 'Retour',
        play: 'Lecture',
        pause: 'Pause',
        seek: 'Position de lecture',
        volume: 'Volume',
        mute: 'Couper le son',
        unmute: 'Rétablir le son',
        speed: 'Vitesse',
        quality: 'Qualité',
        audio: 'Audio',
        subtitles: 'Sous-titres',
        off: 'Désactivés',
        auto: 'Automatique',
        settings: 'Réglages',
        closeSettings: 'Fermer les réglages',
        technical: 'Informations techniques',
        method: 'Mode de lecture',
        transport: 'Transport',
        container: 'Conteneur',
        videoCodec: 'Codec vidéo',
        audioCodec: 'Codec audio',
        source: 'Source',
        sourceBitrate: 'Débit source',
        bitrate: 'Débit maximal',
        shortcuts: 'Raccourcis : espace lecture, ←/→ recul-avance, M son, F plein écran.',
        controls: 'Commandes de lecture',
        loading: 'Préparation de la lecture…',
        fullscreen: 'Plein écran',
        exitFullscreen: 'Quitter le plein écran',
        pip: 'Image dans l’image',
        exitPip: 'Quitter l’image dans l’image',
        skip: 'Passer',
        intro: 'Passer l’introduction',
        recap: 'Passer le récapitulatif',
        credits: 'Passer le générique',
        stillWatching: 'Vous êtes toujours là ?',
        continue: 'Continuer',
        stop: 'Arrêter',
        nextIn: 'Épisode suivant dans {seconds}s',
        errors: {
            codec: 'Ce codec ne peut pas être lu par ce navigateur.',
            network: 'La connexion réseau a été interrompue.',
            'session-expired': 'Votre session a expiré. Reconnectez-vous.',
            'transcoding-unavailable': 'Le serveur ne peut pas fournir de flux compatible.',
            unknown: 'La lecture n’a pas pu démarrer.'
        }
    }
} as const;

export const formatTime = (seconds: number) => {
    if (!Number.isFinite(seconds)) return '0:00';
    const value = Math.max(0, Math.floor(seconds));
    const hours = Math.floor(value / 3600);
    const minutes = Math.floor(value % 3600 / 60);
    const rest = value % 60;
    return hours
        ? `${hours}:${String(minutes).padStart(2, '0')}:${String(rest).padStart(2, '0')}`
        : `${minutes}:${String(rest).padStart(2, '0')}`;
};

export const formatBitrate = (bitsPerSecond?: number | null) => {
    if (!bitsPerSecond || !Number.isFinite(bitsPerSecond)) return '—';
    const megabits = bitsPerSecond / 1_000_000;
    return `${megabits >= 10 ? megabits.toFixed(0) : megabits.toFixed(1)} Mb/s`;
};

export const playbackRestartPosition = (currentTime: number, resumePosition: number) =>
    Number.isFinite(currentTime) && currentTime > 0 ? currentTime : Math.max(0, resumePosition);

export const shouldRetryWithTranscode = (
    mediaErrorCode: number | undefined,
    method: PlaybackMethod | undefined,
    alreadyRetried: boolean
) => !alreadyRetried
    && method !== undefined
    && method !== 'Transcode'
    && (mediaErrorCode === 3 || mediaErrorCode === 4);

type WebKitVideoElement = HTMLVideoElement & {
    webkitDisplayingFullscreen?: boolean;
    webkitEnterFullscreen?(): void;
    webkitPresentationMode?: 'fullscreen' | 'inline' | 'picture-in-picture';
    webkitSetPresentationMode?(mode: 'fullscreen' | 'inline' | 'picture-in-picture'): void;
    webkitSupportsPresentationMode?(mode: 'fullscreen' | 'inline' | 'picture-in-picture'): boolean;
};

export type PlayerShortcut =
    | 'fullscreen'
    | 'mute'
    | 'pip'
    | 'play'
    | 'seek-back'
    | 'seek-forward'
    | 'volume-down'
    | 'volume-up';

export function playerShortcut(key: string): PlayerShortcut | undefined {
    switch (key.toLowerCase()) {
        case ' ':
        case 'k':
        case 'spacebar':
            return 'play';
        case 'arrowleft':
        case 'j':
            return 'seek-back';
        case 'arrowright':
        case 'l':
            return 'seek-forward';
        case 'arrowup':
            return 'volume-up';
        case 'arrowdown':
            return 'volume-down';
        case 'm':
            return 'mute';
        case 'f':
            return 'fullscreen';
        case 'p':
            return 'pip';
        default:
            return undefined;
    }
}

const classifyError = (error: unknown): PlaybackErrorCode => {
    const status = (error as { status?: number; response?: { status?: number } })?.status
        ?? (error as { response?: { status?: number } })?.response?.status;
    if (status === 401) return 'session-expired';
    if (!navigator.onLine) return 'network';
    const message = error instanceof Error ? error.message : String(error);
    if (/TranscodingUnavailable|NoCompatibleStream/i.test(message)) {
        return 'transcoding-unavailable';
    }
    if (/codec|not supported|NoMedia/i.test(message)) return 'codec';
    if (/network|fetch/i.test(message)) return 'network';
    return 'unknown';
};

const streamLabel = (stream: MediaStream) =>
    stream.DisplayTitle || stream.Title || stream.Language || `#${stream.Index ?? ''}`;

export interface WatchPlayerProps {
    client: PlaybackHttpClient;
    item: PlayerItem;
    userId: string;
    profileId: string;
    settings: ProfilePlaybackSettings;
    preferences: PlaybackPreferences;
    resumePositionSeconds?: number;
    locale?: 'en' | 'fr';
    onClose?(): void;
    onPlayNext?(itemId: string, resumePositionSeconds: number): void;
    onSessionExpired?(): void;
}

interface PlaybackOverrides {
    audioStreamIndex?: number | undefined;
    bitrate?: number | undefined;
    compatibilityRetry?: boolean | undefined;
    disableSubtitles?: boolean | undefined;
    forceFullTranscode?: boolean | undefined;
    forceSubtitleBurnIn?: boolean | undefined;
    forceTranscode?: boolean | undefined;
    subtitleStreamIndex?: number | undefined;
}

export function WatchPlayer({
    client,
    item,
    userId,
    profileId,
    settings,
    preferences,
    resumePositionSeconds = 0,
    locale = 'en',
    onClose,
    onPlayNext,
    onSessionExpired
}: WatchPlayerProps) {
    const labels = copy[locale];
    const snapshot = usePlayerState();
    const stageRef = useRef<HTMLElement>(null);
    const videoRef = useRef<HTMLVideoElement>(null);
    const settingsButtonRef = useRef<HTMLButtonElement>(null);
    const generation = useRef(0);
    const compatibilityRetryAttempted = useRef(false);
    const subtitleRetryAttempted = useRef(false);
    const autoplayController = useRef<AbortController | undefined>(undefined);
    const fatalPlaybackRef = useRef<(error: PlaybackErrorCode, mediaErrorCode?: number) => void>(
        () => undefined
    );
    const subtitleFailureRef = useRef<() => void>(() => undefined);
    const abortController = useRef<AbortController | undefined>(undefined);
    const attachedMedia = useRef<AttachedMedia | undefined>(undefined);
    const attachedSubtitle = useRef<AttachedSubtitle | undefined>(undefined);
    const reporter = useRef<ProgressReporter | undefined>(undefined);
    const nextEpisode = useRef<AutoplayDecision | null>(null);
    const [ mediaSource, setMediaSource ] = useState<MediaSource>();
    const [ selectedAudio, setSelectedAudio ] = useState<number>();
    const [ selectedSubtitle, setSelectedSubtitle ] = useState<number>();
    const [ maxBitrate, setMaxBitrate ] = useState(0);
    const [ segments, setSegments ] = useState<Awaited<ReturnType<typeof loadMediaSegments>>>([]);
    const [ countdown, setCountdown ] = useState<number>();
    const [ stillWatching, setStillWatching ] = useState(false);
    const [ settingsOpen, setSettingsOpen ] = useState(false);
    const [ muted, setMuted ] = useState(false);
    const [ fullscreen, setFullscreen ] = useState(false);
    const [ pictureInPicture, setPictureInPicture ] = useState(false);
    const [ pictureInPictureAvailable, setPictureInPictureAvailable ] = useState(false);
    const volumeAdjustable = !(/iP(?:hone|ad|od)/i.test(navigator.userAgent)
        || navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    const closeSettings = useCallback(() => {
        setSettingsOpen(false);
        window.requestAnimationFrame(() => settingsButtonRef.current?.focus());
    }, []);
    const streams = mediaSource?.MediaStreams ?? EMPTY_STREAMS;
    const audioStreams = useMemo(
        () => streams.filter(stream => stream.Type === 'Audio' && stream.Index != null),
        [ streams ]
    );
    const subtitleStreams = useMemo(
        () => streams.filter(stream => stream.Type === 'Subtitle' && stream.Index != null),
        [ streams ]
    );
    const videoStream = streams.find(stream => stream.Type === 'Video');
    const selectedAudioStream = audioStreams.find(stream => stream.Index === selectedAudio);
    const videoDescription = [
        videoStream?.Codec?.toUpperCase(),
        videoStream?.Profile,
        videoStream?.Width && videoStream?.Height
            ? `${videoStream.Width}×${videoStream.Height}`
            : undefined,
        videoStream?.BitDepth ? `${videoStream.BitDepth}-bit` : undefined,
        videoStream?.VideoRangeType,
        videoStream?.AverageFrameRate || videoStream?.RealFrameRate
            ? `${(videoStream.AverageFrameRate ?? videoStream.RealFrameRate)?.toFixed(2)} fps`
            : undefined
    ].filter(Boolean).join(' · ') || '—';
    const audioDescription = [
        selectedAudioStream?.Codec?.toUpperCase(),
        selectedAudioStream?.Profile,
        selectedAudioStream?.Channels ? `${selectedAudioStream.Channels} ch` : undefined,
        selectedAudioStream?.SampleRate
            ? `${(selectedAudioStream.SampleRate / 1_000).toFixed(1)} kHz`
            : undefined,
        selectedAudioStream?.BitRate ? formatBitrate(selectedAudioStream.BitRate) : undefined
    ].filter(Boolean).join(' · ') || '—';
    const playbackMethod = snapshot.selection?.method === 'DirectPlay'
        ? 'Direct Play'
        : snapshot.selection?.method === 'DirectStream'
            ? 'Remux'
            : snapshot.selection?.method === 'Transcode'
                ? locale === 'fr' ? 'Transcodage' : 'Transcode'
                : '—';
    const activeSegment = findActiveSegment(segments, snapshot.currentTime);

    const cleanupPlayback = useCallback(() => {
        reporter.current?.stop();
        reporter.current = undefined;
        attachedMedia.current?.destroy();
        attachedMedia.current = undefined;
        attachedSubtitle.current?.dispose();
        attachedSubtitle.current = undefined;
    }, []);

    const attachSelectedSubtitle = useCallback(async (
        source: MediaSource,
        subtitleIndex: number | undefined,
        currentGeneration: number
    ) => {
        attachedSubtitle.current?.dispose();
        attachedSubtitle.current = undefined;
        if (subtitleIndex == null) return;
        const stream = source.MediaStreams?.find(candidate => candidate.Index === subtitleIndex);
        if (!stream?.DeliveryUrl || (stream.DeliveryMethod
            && stream.DeliveryMethod.toLowerCase() !== 'external')) return;
        const renderer = await attachSubtitle(
            videoRef.current!,
            stream,
            client.mediaUrl(stream.DeliveryUrl),
            () => subtitleFailureRef.current()
        );
        if (currentGeneration !== generation.current) {
            renderer?.dispose();
            return;
        }
        attachedSubtitle.current = renderer ?? undefined;
    }, [ client ]);

    const startPlayback = useCallback(async (overrides: PlaybackOverrides = {}) => {
        const video = videoRef.current;
        if (!video) return;
        const restartPosition = playbackRestartPosition(video.currentTime, resumePositionSeconds);
        if (!overrides.compatibilityRetry) compatibilityRetryAttempted.current = false;
        if (!overrides.forceSubtitleBurnIn) subtitleRetryAttempted.current = false;
        const currentGeneration = ++generation.current;
        abortController.current?.abort();
        abortController.current = new AbortController();
        cleanupPlayback();
        playerStore.set({
            status: 'loading',
            error: undefined,
            item,
            profileId,
            currentTime: restartPosition
        });

        try {
            const effectivePreferences: PlaybackPreferences = {
                ...preferences,
                MaxStreamingBitrate: overrides.bitrate || preferences.MaxStreamingBitrate,
                SubtitlesEnabled: overrides.disableSubtitles
                    ? false
                    : preferences.SubtitlesEnabled
            };
            const startTicks = restartPosition * 10_000_000;
            let request = buildPlaybackRequest(
                userId,
                effectivePreferences,
                startTicks,
                mediaSource?.MediaStreams,
                { ...overrides, mediaSourceId: mediaSource?.Id ?? undefined }
            );
            if (overrides.forceTranscode || overrides.forceFullTranscode) {
                request.EnableDirectPlay = false;
            }
            if (overrides.forceFullTranscode) {
                request.EnableDirectStream = false;
                request.AllowVideoStreamCopy = false;
                request.AllowAudioStreamCopy = false;
            }
            let response = await client.request<PlaybackInfoResponse>(
                `Items/${encodeURIComponent(item.Id)}/PlaybackInfo`,
                {
                    ...({ method: 'POST', body: JSON.stringify(request) } satisfies RequestInit),
                    headers: { 'Content-Type': 'application/json' },
                    signal: abortController.current.signal
                }
            );
            if (currentGeneration !== generation.current) return;

            let source = selectPlaybackSource(
                response,
                item.Id,
                { ...effectivePreferences, PreferDirectPlay: request.EnableDirectPlay },
                client.mediaUrl.bind(client)
            ).source;
            const audioStreamIndex = overrides.audioStreamIndex ?? chooseStream(
                source.MediaStreams,
                'Audio',
                preferences.PreferredAudioLanguage,
                preferences.AudioDescriptionEnabled
            ) ?? source.DefaultAudioStreamIndex ?? undefined;
            const subtitleStreamIndex = chooseSubtitleStream(
                source.MediaStreams,
                preferences,
                overrides.subtitleStreamIndex,
                overrides.disableSubtitles
            ) ?? (!overrides.disableSubtitles && preferences.SubtitlesEnabled
                ? source.DefaultSubtitleStreamIndex ?? undefined
                : undefined);
            const needsSelectedAudio = audioStreamIndex != null
                && audioStreamIndex !== source.DefaultAudioStreamIndex;
            const subtitleStream = source.MediaStreams?.find(stream =>
                stream.Index === subtitleStreamIndex);
            const needsBurnedSubtitle = subtitleStreamIndex != null
                && (subtitleStream?.DeliveryMethod?.toLowerCase() !== 'external'
                    || subtitleStream?.IsExternalUrl === true);
            if (needsSelectedAudio
                || needsBurnedSubtitle
                || overrides.audioStreamIndex != null
                || overrides.bitrate
                || overrides.forceTranscode
                || overrides.forceFullTranscode) {
                request = buildPlaybackRequest(
                    userId,
                    effectivePreferences,
                    startTicks,
                    source.MediaStreams,
                    {
                        audioStreamIndex,
                        forceSubtitleBurnIn: overrides.forceSubtitleBurnIn
                            || subtitleStream?.IsExternalUrl === true,
                        mediaSourceId: source.Id ?? undefined,
                        subtitleStreamIndex
                    }
                );
                if (needsSelectedAudio
                    || needsBurnedSubtitle
                    || overrides.forceTranscode
                    || overrides.forceFullTranscode) {
                    request.EnableDirectPlay = false;
                }
                if (overrides.forceFullTranscode) {
                    request.EnableDirectStream = false;
                    request.AllowVideoStreamCopy = false;
                    request.AllowAudioStreamCopy = false;
                }
                response = await client.request<PlaybackInfoResponse>(
                    `Items/${encodeURIComponent(item.Id)}/PlaybackInfo`,
                    {
                        method: 'POST',
                        body: JSON.stringify(request),
                        headers: { 'Content-Type': 'application/json' },
                        signal: abortController.current.signal
                    }
                );
                source = selectPlaybackSource(
                    response,
                    item.Id,
                    { ...effectivePreferences, PreferDirectPlay: request.EnableDirectPlay },
                    client.mediaUrl.bind(client),
                    { audioStreamIndex, subtitleStreamIndex }
                ).source;
            }

            const selection = selectPlaybackSource(
                response,
                item.Id,
                { ...effectivePreferences, PreferDirectPlay: request.EnableDirectPlay },
                client.mediaUrl.bind(client),
                { audioStreamIndex, subtitleStreamIndex }
            );
            setMediaSource(selection.source);
            setSelectedAudio(audioStreamIndex);
            setSelectedSubtitle(subtitleStreamIndex);
            playerStore.set({
                selection,
                playSessionId: response.PlaySessionId,
                duration: (selection.source.RunTimeTicks ?? item.RunTimeTicks ?? 0) / 10_000_000
            });
            const media = await attachMedia(
                video,
                selection.url,
                selection.isHls,
                client.authHeaders(),
                error => {
                    if (currentGeneration === generation.current) {
                        fatalPlaybackRef.current(error, error === 'codec' ? 4 : 2);
                    }
                },
                { startPositionSeconds: restartPosition }
            );
            if (currentGeneration !== generation.current) {
                media.destroy();
                return;
            }
            attachedMedia.current = media;
            await attachSelectedSubtitle(selection.source, subtitleStreamIndex, currentGeneration);
            if (currentGeneration !== generation.current) return;
            try {
                await video.play();
            } catch (error) {
                if ((error as { name?: string })?.name !== 'NotAllowedError') throw error;
                playerStore.set({ status: 'paused' });
            }

            const progressReporter = new ProgressReporter({
                client,
                item,
                profileId,
                playSessionId: response.PlaySessionId,
                selection,
                readState: () => ({
                    currentTime: video.currentTime,
                    duration: video.duration || (selection.source.RunTimeTicks ?? item.RunTimeTicks ?? 0) / 10_000_000,
                    paused: video.paused,
                    muted: video.muted,
                    volume: video.volume,
                    playbackRate: video.playbackRate
                }),
                onSessionExpired: () => {
                    playerStore.set({ error: 'session-expired', status: 'error' });
                    onSessionExpired?.();
                }
            });
            reporter.current = progressReporter;
            void progressReporter.start().catch(() => undefined);
        } catch (error) {
            if ((error as { name?: string })?.name === 'AbortError') return;
            if (currentGeneration !== generation.current) return;
            playerStore.set({ error: classifyError(error), status: 'error' });
        }
    }, [
        attachSelectedSubtitle,
        cleanupPlayback,
        client,
        item,
        mediaSource?.Id,
        mediaSource?.MediaStreams,
        onSessionExpired,
        preferences,
        profileId,
        resumePositionSeconds,
        userId
    ]);

    const handleFatalPlayback = useCallback((
        error: PlaybackErrorCode,
        mediaErrorCode?: number
    ) => {
        const selection = playerStore.getSnapshot().selection;
        if (error === 'codec' && shouldRetryWithTranscode(
            mediaErrorCode,
            selection?.method,
            compatibilityRetryAttempted.current
        )) {
            compatibilityRetryAttempted.current = true;
            reporter.current?.stop();
            void startPlayback({
                audioStreamIndex: selection?.audioStreamIndex,
                bitrate: maxBitrate,
                compatibilityRetry: true,
                disableSubtitles: selection?.subtitleStreamIndex == null,
                forceFullTranscode: true,
                subtitleStreamIndex: selection?.subtitleStreamIndex
            });
            return;
        }
        reporter.current?.stop();
        playerStore.set({ error, status: 'error' });
    }, [ maxBitrate, startPlayback ]);

    const handleSubtitleFailure = useCallback(() => {
        const selection = playerStore.getSnapshot().selection;
        if (!selection || subtitleRetryAttempted.current) {
            handleFatalPlayback('codec', 4);
            return;
        }
        subtitleRetryAttempted.current = true;
        reporter.current?.stop();
        void startPlayback({
            audioStreamIndex: selection.audioStreamIndex,
            bitrate: maxBitrate,
            compatibilityRetry: true,
            forceSubtitleBurnIn: true,
            forceTranscode: true,
            subtitleStreamIndex: selection.subtitleStreamIndex
        });
    }, [ handleFatalPlayback, maxBitrate, startPlayback ]);

    useEffect(() => {
        fatalPlaybackRef.current = handleFatalPlayback;
        subtitleFailureRef.current = handleSubtitleFailure;
    }, [ handleFatalPlayback, handleSubtitleFailure ]);

    useEffect(() => {
        void startPlayback();
        return () => {
            // The generation counter deliberately invalidates every pending async attachment.
            // eslint-disable-next-line react-hooks/exhaustive-deps
            ++generation.current;
            abortController.current?.abort();
            autoplayController.current?.abort();
            cleanupPlayback();
            playerStore.reset();
        };
        // Playback is intentionally restarted only for a different item/profile.
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [ item.Id, profileId ]);

    useEffect(() => {
        if (!mediaSource || mediaSource.HasSegments === false) {
            return;
        }
        const controller = new AbortController();
        void loadMediaSegments(
            client,
            mediaSource.Id || item.Id,
            settings,
            preferences,
            controller.signal
        )
            .then(setSegments);
        return () => controller.abort();
    }, [ client, item.Id, mediaSource, preferences, settings ]);

    const requestAutoplayAction = useCallback(async () => {
        autoplayController.current?.abort();
        const controller = new AbortController();
        autoplayController.current = controller;
        try {
            return await getAutoplayAction(
                client,
                profileId,
                item.Id,
                controller.signal
            );
        } catch (error) {
            if ((error as { name?: string })?.name !== 'AbortError') {
                nextEpisode.current = null;
            }
            return null;
        }
    }, [ client, item.Id, profileId ]);

    const applyAutoplayAction = useCallback((action: AutoplayAction | null) => {
        if (!action) {
            nextEpisode.current = null;
            return;
        }
        if (action.type === 'confirm') {
            nextEpisode.current = action.pendingDecision;
            setStillWatching(true);
            return;
        }
        nextEpisode.current = action.decision;
        setCountdown(action.decision.delaySeconds);
    }, []);

    useEffect(() => {
        if (countdown == null || countdown <= 0) {
            if (countdown === 0 && nextEpisode.current) {
                onPlayNext?.(
                    nextEpisode.current.itemId,
                    nextEpisode.current.resumePositionSeconds
                );
            }
            return;
        }
        const timer = window.setTimeout(() => setCountdown(value => Math.max(0, (value ?? 1) - 1)), 1000);
        return () => window.clearTimeout(timer);
    }, [ countdown, onPlayNext ]);

    const onEnded = async () => {
        playerStore.set({ status: 'ended' });
        const activeReporter = reporter.current;
        await activeReporter?.report(true);
        activeReporter?.stop();
        nextEpisode.current = null;
        if (!settings.AutoplayEnabled || item.Type !== 'Episode') return;
        applyAutoplayAction(await requestAutoplayAction());
    };

    const onVideoError = () => {
        const code = videoRef.current?.error?.code;
        handleFatalPlayback(
            code === MediaError.MEDIA_ERR_NETWORK ? 'network' : 'codec',
            code
        );
    };

    const switchSubtitle = async (value: string) => {
        const index = value === '' ? undefined : Number(value);
        const previous = mediaSource?.MediaStreams?.find(candidate =>
            candidate.Index === selectedSubtitle);
        setSelectedSubtitle(index);
        const stream = mediaSource?.MediaStreams?.find(candidate => candidate.Index === index);
        if (index == null
            && previous
            && previous.DeliveryMethod?.toLowerCase() !== 'external') {
            await startPlayback({
                audioStreamIndex: selectedAudio,
                bitrate: maxBitrate,
                disableSubtitles: true
            });
            return;
        }
        if (stream && (stream.DeliveryMethod?.toLowerCase() !== 'external'
            || stream.IsExternalUrl === true)) {
            await startPlayback({
                audioStreamIndex: selectedAudio,
                subtitleStreamIndex: index,
                bitrate: maxBitrate,
                forceSubtitleBurnIn: stream.IsExternalUrl === true,
                forceTranscode: true
            });
            return;
        }
        if (mediaSource) {
            await attachSelectedSubtitle(mediaSource, index, generation.current);
        }
        reporter.current?.setSubtitleStreamIndex(index);
        if (snapshot.selection) {
            playerStore.set({
                selection: { ...snapshot.selection, subtitleStreamIndex: index }
            });
        }
        void reporter.current?.report(true);
    };

    const skipSegment = () => {
        const end = activeSegment?.EndTicks;
        if (!end || !videoRef.current) return;
        videoRef.current.currentTime = end / 10_000_000;
        void reporter.current?.report(true);
    };

    const togglePlayback = useCallback(() => {
        const video = videoRef.current;
        if (!video) return;
        if (video.paused) void video.play();
        else video.pause();
    }, []);

    const seekBy = useCallback((seconds: number) => {
        const video = videoRef.current;
        if (!video) return;
        const duration = Number.isFinite(video.duration) ? video.duration : snapshot.duration;
        video.currentTime = Math.min(Math.max(0, video.currentTime + seconds), duration || Number.MAX_SAFE_INTEGER);
    }, [ snapshot.duration ]);

    const changeVolume = useCallback((difference: number) => {
        const video = videoRef.current;
        if (!video) return;
        video.volume = Math.min(1, Math.max(0, video.volume + difference));
        if (difference > 0 && video.muted) video.muted = false;
    }, []);

    const toggleMute = useCallback(() => {
        const video = videoRef.current;
        if (!video) return;
        video.muted = !video.muted;
        setMuted(video.muted);
        void reporter.current?.report(true);
    }, []);

    const toggleFullscreen = useCallback(() => {
        const video = videoRef.current as WebKitVideoElement | null;
        if (document.fullscreenElement) {
            void document.exitFullscreen().catch(() => undefined);
        } else if (stageRef.current?.requestFullscreen) {
            void stageRef.current.requestFullscreen().catch(() => undefined);
        } else {
            video?.webkitEnterFullscreen?.();
        }
    }, []);

    const togglePictureInPicture = useCallback(() => {
        const video = videoRef.current as WebKitVideoElement | null;
        if (!video) return;
        if (document.pictureInPictureEnabled && document.pictureInPictureElement) {
            void document.exitPictureInPicture().catch(() => undefined);
        } else if (document.pictureInPictureEnabled && video.requestPictureInPicture) {
            void video.requestPictureInPicture().catch(() => undefined);
        } else if (video.webkitSupportsPresentationMode?.('picture-in-picture')) {
            video.webkitSetPresentationMode?.(
                video.webkitPresentationMode === 'picture-in-picture'
                    ? 'inline'
                    : 'picture-in-picture'
            );
        }
    }, []);

    useEffect(() => {
        const onFullscreenChange = () => setFullscreen(Boolean(document.fullscreenElement));
        const video = videoRef.current as WebKitVideoElement | null;
        const onEnterPictureInPicture = () => setPictureInPicture(true);
        const onLeavePictureInPicture = () => setPictureInPicture(false);
        const onWebkitPresentationChange = () => {
            setPictureInPicture(video?.webkitPresentationMode === 'picture-in-picture');
        };
        const onWebkitBeginFullscreen = () => setFullscreen(true);
        const onWebkitEndFullscreen = () => setFullscreen(false);
        setPictureInPictureAvailable(Boolean(
            document.pictureInPictureEnabled
            || video?.webkitSupportsPresentationMode?.('picture-in-picture')
        ));
        document.addEventListener('fullscreenchange', onFullscreenChange);
        video?.addEventListener('enterpictureinpicture', onEnterPictureInPicture);
        video?.addEventListener('leavepictureinpicture', onLeavePictureInPicture);
        video?.addEventListener('webkitpresentationmodechanged', onWebkitPresentationChange);
        video?.addEventListener('webkitbeginfullscreen', onWebkitBeginFullscreen);
        video?.addEventListener('webkitendfullscreen', onWebkitEndFullscreen);
        return () => {
            document.removeEventListener('fullscreenchange', onFullscreenChange);
            video?.removeEventListener('enterpictureinpicture', onEnterPictureInPicture);
            video?.removeEventListener('leavepictureinpicture', onLeavePictureInPicture);
            video?.removeEventListener('webkitpresentationmodechanged', onWebkitPresentationChange);
            video?.removeEventListener('webkitbeginfullscreen', onWebkitBeginFullscreen);
            video?.removeEventListener('webkitendfullscreen', onWebkitEndFullscreen);
        };
    }, []);

    useEffect(() => {
        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === 'Escape' && settingsOpen) {
                event.preventDefault();
                closeSettings();
                return;
            }
            if (event.altKey || event.ctrlKey || event.metaKey) return;
            const target = event.target;
            if (target instanceof HTMLElement
                && target.closest('button, input, select, textarea, [contenteditable="true"]')) return;
            const shortcut = playerShortcut(event.key);
            if (!shortcut) return;
            event.preventDefault();
            switch (shortcut) {
                case 'play':
                    togglePlayback();
                    break;
                case 'seek-back':
                    seekBy(-10);
                    break;
                case 'seek-forward':
                    seekBy(10);
                    break;
                case 'volume-up':
                    changeVolume(.1);
                    break;
                case 'volume-down':
                    changeVolume(-.1);
                    break;
                case 'mute':
                    toggleMute();
                    break;
                case 'fullscreen':
                    toggleFullscreen();
                    break;
                case 'pip':
                    togglePictureInPicture();
                    break;
            }
        };
        document.addEventListener('keydown', onKeyDown);
        return () => document.removeEventListener('keydown', onKeyDown);
    }, [
        changeVolume,
        closeSettings,
        seekBy,
        settingsOpen,
        toggleFullscreen,
        toggleMute,
        togglePictureInPicture,
        togglePlayback
    ]);

    const segmentLabel = activeSegment?.Type === 'Intro'
        ? labels.intro
        : activeSegment?.Type === 'Recap'
            ? labels.recap
            : labels.credits;

    return (
        <main className={styles.root} aria-label={item.Name || 'Player'}>
            <section className={styles.stage} ref={stageRef}>
                <video
                    ref={videoRef}
                    className={styles.video}
                    playsInline
                    onDurationChange={event => playerStore.set({ duration: event.currentTarget.duration || snapshot.duration })}
                    onEnded={onEnded}
                    onError={onVideoError}
                    onPause={() => {
                        playerStore.set({ status: 'paused' });
                        void reporter.current?.report(true);
                    }}
                    onPlay={() => playerStore.set({ status: 'playing' })}
                    onRateChange={event => playerStore.set({ playbackRate: event.currentTarget.playbackRate })}
                    onSeeked={() => void reporter.current?.report(true)}
                    onTimeUpdate={event => playerStore.set({ currentTime: event.currentTarget.currentTime })}
                    onVolumeChange={event => {
                        playerStore.set({ volume: event.currentTarget.volume });
                        setMuted(event.currentTarget.muted);
                    }}
                />

                <header className={styles.top}>
                    <button className={styles.button} type="button" onClick={onClose} aria-label={labels.back}>
                        ←
                    </button>
                    <h1 className={styles.title}>{item.Name}</h1>
                </header>

                {activeSegment && (
                    <button className={styles.skip} type="button" onClick={skipSegment}>
                        {segmentLabel}
                    </button>
                )}

                {snapshot.error && (
                    <div className={styles.error} role="alert">
                        <p>{labels.errors[snapshot.error]}</p>
                        <p className={styles.errorCode}>{snapshot.error}</p>
                        <button className={styles.button} type="button" onClick={() => void startPlayback()}>
                            {labels.play}
                        </button>
                    </div>
                )}

                {snapshot.status === 'loading' && !snapshot.error && (
                    <div className={styles.loading} role="status">
                        <span aria-hidden="true" />
                        {labels.loading}
                    </div>
                )}

                {stillWatching && (
                    <div className={styles.dialog} role="dialog" aria-modal="true" aria-labelledby="still-watching-title">
                        <h2 id="still-watching-title">{labels.stillWatching}</h2>
                        <div className={styles.dialogActions}>
                            <button
                                className={styles.button}
                                type="button"
                                onClick={() => {
                                    void (async () => {
                                        try {
                                            await confirmStillWatching(client, profileId);
                                            setStillWatching(false);
                                            applyAutoplayAction(await requestAutoplayAction());
                                        } catch (error) {
                                            setStillWatching(false);
                                            playerStore.set({
                                                error: classifyError(error),
                                                status: 'error'
                                            });
                                        }
                                    })();
                                }}
                            >
                                {labels.continue}
                            </button>
                            <button className={styles.button} type="button" onClick={onClose}>
                                {labels.stop}
                            </button>
                        </div>
                    </div>
                )}

                {countdown != null && countdown > 0 && (
                    <div className={styles.dialog} role="status">
                        <p>{labels.nextIn.replace('{seconds}', String(countdown))}</p>
                        <button
                            className={styles.button}
                            type="button"
                            onClick={() => setCountdown(undefined)}
                        >
                            {labels.stop}
                        </button>
                    </div>
                )}

                <div aria-label={labels.controls} className={styles.controls} role="group">
                    <div className={styles.timeline}>
                        <input
                            aria-label={labels.seek}
                            aria-valuetext={`${formatTime(snapshot.currentTime)} / ${formatTime(snapshot.duration)}`}
                            className={styles.seek}
                            max={Math.max(0, snapshot.duration)}
                            min="0"
                            onChange={event => {
                                if (videoRef.current) videoRef.current.currentTime = Number(event.target.value);
                            }}
                            step=".25"
                            type="range"
                            value={Math.min(snapshot.currentTime, snapshot.duration || 0)}
                        />
                    </div>
                    <div className={styles.controlRow}>
                        <button
                            aria-label={snapshot.status === 'playing' ? labels.pause : labels.play}
                            className={`${styles.button} ${styles.primaryControl}`}
                            onClick={togglePlayback}
                            type="button"
                        >
                            {snapshot.status === 'playing' ? 'Ⅱ' : '▶'}
                        </button>
                        <button
                            aria-label={muted || snapshot.volume === 0 ? labels.unmute : labels.mute}
                            aria-pressed={muted}
                            className={styles.button}
                            onClick={toggleMute}
                            type="button"
                        >
                            {muted || snapshot.volume === 0 ? 'MUTE' : 'VOL'}
                        </button>
                        {volumeAdjustable && (
                            <input
                                aria-label={labels.volume}
                                className={styles.volume}
                                max="1"
                                min="0"
                                onChange={event => {
                                    const video = videoRef.current;
                                    if (!video) return;
                                    video.volume = Number(event.target.value);
                                    if (video.volume > 0 && video.muted) video.muted = false;
                                }}
                                step=".05"
                                type="range"
                                value={snapshot.volume}
                            />
                        )}
                        <output className={styles.time}>
                            {formatTime(snapshot.currentTime)} / {formatTime(snapshot.duration)}
                        </output>
                        <span className={styles.spacer} />
                        <button
                            aria-controls="player-settings"
                            aria-expanded={settingsOpen}
                            aria-label={labels.settings}
                            className={styles.button}
                            onClick={() => setSettingsOpen(value => !value)}
                            ref={settingsButtonRef}
                            type="button"
                        >
                            •••
                        </button>
                        {pictureInPictureAvailable && (
                            <button
                                aria-label={pictureInPicture ? labels.exitPip : labels.pip}
                                aria-pressed={pictureInPicture}
                                className={`${styles.button} ${styles.pipButton}`}
                                onClick={togglePictureInPicture}
                                type="button"
                            >
                                PIP
                            </button>
                        )}
                        <button
                            aria-label={fullscreen ? labels.exitFullscreen : labels.fullscreen}
                            aria-pressed={fullscreen}
                            className={styles.button}
                            onClick={toggleFullscreen}
                            type="button"
                        >
                            ⛶
                        </button>
                    </div>

                    {settingsOpen && (
                        <section
                            aria-labelledby="player-settings-title"
                            className={styles.settings}
                            id="player-settings"
                        >
                            <header>
                                <h2 id="player-settings-title">{labels.settings}</h2>
                                <button
                                    aria-label={labels.closeSettings}
                                    className={styles.button}
                                    onClick={closeSettings}
                                    type="button"
                                >
                                    ×
                                </button>
                            </header>
                            <div className={styles.settingsGrid}>
                                <label>
                                    <span>{labels.speed}</span>
                                    <select
                                        className={styles.select}
                                        onChange={event => {
                                            if (videoRef.current) videoRef.current.playbackRate = Number(event.target.value);
                                        }}
                                        value={snapshot.playbackRate}
                                    >
                                        {[ .5, .75, 1, 1.25, 1.5, 2 ].map(rate => (
                                            <option key={rate} value={rate}>{rate}×</option>
                                        ))}
                                    </select>
                                </label>
                                <label>
                                    <span>{labels.quality}</span>
                                    <select
                                        className={styles.select}
                                        onChange={event => {
                                            const bitrate = Number(event.target.value);
                                            setMaxBitrate(bitrate);
                                            void startPlayback({
                                                audioStreamIndex: selectedAudio,
                                                disableSubtitles: selectedSubtitle == null,
                                                subtitleStreamIndex: selectedSubtitle,
                                                bitrate
                                            });
                                        }}
                                        value={maxBitrate}
                                    >
                                        <option value="0">{labels.auto}</option>
                                        {QUALITY_OPTIONS.slice(1).map(option => (
                                            <option key={option.value} value={option.value}>{option.label}</option>
                                        ))}
                                    </select>
                                </label>
                                <label>
                                    <span>{labels.audio}</span>
                                    <select
                                        className={styles.select}
                                        disabled={!audioStreams.length}
                                        onChange={event => {
                                            const index = Number(event.target.value);
                                            setSelectedAudio(index);
                                            void startPlayback({
                                                audioStreamIndex: index,
                                                disableSubtitles: selectedSubtitle == null,
                                                subtitleStreamIndex: selectedSubtitle,
                                                bitrate: maxBitrate
                                            });
                                        }}
                                        value={selectedAudio ?? ''}
                                    >
                                        {!audioStreams.length && <option value="">{labels.auto}</option>}
                                        {audioStreams.map(stream => (
                                            <option key={stream.Index} value={stream.Index ?? ''}>
                                                {streamLabel(stream)}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                                <label>
                                    <span>{labels.subtitles}</span>
                                    <select
                                        className={styles.select}
                                        onChange={event => void switchSubtitle(event.target.value)}
                                        value={selectedSubtitle ?? ''}
                                    >
                                        <option value="">{labels.off}</option>
                                        {subtitleStreams.map(stream => (
                                            <option key={stream.Index} value={stream.Index ?? ''}>
                                                {streamLabel(stream)}
                                            </option>
                                        ))}
                                    </select>
                                </label>
                            </div>
                            <details className={styles.technical}>
                                <summary>{labels.technical}</summary>
                                <dl>
                                    <div><dt>{labels.method}</dt><dd>{playbackMethod}</dd></div>
                                    <div>
                                        <dt>{labels.transport}</dt>
                                        <dd>{snapshot.selection ? snapshot.selection.isHls ? 'HLS' : 'HTTP' : '—'}</dd>
                                    </div>
                                    <div><dt>{labels.container}</dt><dd>{mediaSource?.Container ?? '—'}</dd></div>
                                    <div><dt>{labels.videoCodec}</dt><dd>{videoDescription}</dd></div>
                                    <div><dt>{labels.audioCodec}</dt><dd>{audioDescription}</dd></div>
                                    <div><dt>{labels.source}</dt><dd>{mediaSource?.Name ?? '—'}</dd></div>
                                    <div>
                                        <dt>{labels.sourceBitrate}</dt>
                                        <dd>{formatBitrate(mediaSource?.Bitrate)}</dd>
                                    </div>
                                    <div>
                                        <dt>{labels.bitrate}</dt>
                                        <dd>{maxBitrate ? formatBitrate(maxBitrate) : labels.auto}</dd>
                                    </div>
                                </dl>
                            </details>
                            <p className={styles.shortcuts}>{labels.shortcuts}</p>
                        </section>
                    )}
                </div>
            </section>
        </main>
    );
}
