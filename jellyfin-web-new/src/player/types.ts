export const TICKS_PER_SECOND = 10_000_000;

export type PlaybackMethod = 'DirectPlay' | 'DirectStream' | 'Transcode';
export type PlaybackErrorCode =
    | 'codec'
    | 'network'
    | 'session-expired'
    | 'transcoding-unavailable'
    | 'unknown';

export interface PlaybackPreferences {
    PreferDirectPlay: boolean;
    AllowContainerRemuxing: boolean;
    AllowVideoTranscoding: boolean;
    AllowAudioTranscoding: boolean;
    PreferHardwareTranscoding: boolean;
    MaxStreamingBitrate?: number | null | undefined;
    PreferredAudioLanguage?: string | null | undefined;
    PreferredSubtitleLanguage?: string | null | undefined;
    SubtitlesEnabled: boolean;
    AudioDescriptionEnabled: boolean;
    ClosedCaptionsEnabled: boolean;
    SkipCreditsEnabled: boolean;
}

export interface ProfilePlaybackSettings {
    AutoplayEnabled: boolean;
    AutoplayDelaySeconds: number;
    SkipIntroEnabled: boolean;
    SkipRecapEnabled: boolean;
}

export interface MediaStream {
    Index?: number | null;
    Type?: string | null;
    Codec?: string | null;
    Profile?: string | null;
    BitRate?: number | null;
    BitDepth?: number | null;
    Width?: number | null;
    Height?: number | null;
    AverageFrameRate?: number | null;
    RealFrameRate?: number | null;
    VideoRangeType?: string | null;
    Channels?: number | null;
    ChannelLayout?: string | null;
    SampleRate?: number | null;
    Language?: string | null;
    Title?: string | null;
    DisplayTitle?: string | null;
    IsDefault?: boolean | null;
    IsForced?: boolean | null;
    IsExternal?: boolean | null;
    IsExternalUrl?: boolean | null;
    DeliveryMethod?: string | null;
    DeliveryUrl?: string | null;
}

export interface MediaSource {
    Id?: string | null;
    Name?: string | null;
    Container?: string | null;
    Bitrate?: number | null;
    RunTimeTicks?: number | null;
    SupportsDirectPlay?: boolean;
    SupportsDirectStream?: boolean;
    SupportsTranscoding?: boolean;
    TranscodingUrl?: string | null;
    TranscodingSubProtocol?: string | null;
    TranscodingContainer?: string | null;
    DefaultAudioStreamIndex?: number | null;
    DefaultSubtitleStreamIndex?: number | null;
    HasSegments?: boolean;
    MediaStreams?: MediaStream[] | null;
}

export interface PlaybackInfoResponse {
    MediaSources?: MediaSource[];
    PlaySessionId?: string | null;
    ErrorCode?: string | null;
}

export interface PlaybackSelection {
    method: PlaybackMethod;
    source: MediaSource;
    url: string;
    isHls: boolean;
    audioStreamIndex?: number | undefined;
    subtitleStreamIndex?: number | undefined;
}

export interface PlaybackRequest {
    UserId: string;
    MediaSourceId?: string | undefined;
    MaxStreamingBitrate?: number | null | undefined;
    StartTimeTicks: number;
    AudioStreamIndex?: number | undefined;
    SubtitleStreamIndex?: number | undefined;
    DeviceProfile: Record<string, unknown>;
    EnableDirectPlay: boolean;
    EnableDirectStream: boolean;
    EnableTranscoding: boolean;
    AllowVideoStreamCopy: boolean;
    AllowAudioStreamCopy: boolean;
    AutoOpenLiveStream: false;
}

export interface NativePlaybackReport {
    ItemId: string;
    MediaSourceId?: string | null | undefined;
    PlaySessionId?: string | null | undefined;
    PositionTicks: number;
    IsPaused: boolean;
    IsMuted: boolean;
    VolumeLevel: number;
    PlayMethod: PlaybackMethod;
    AudioStreamIndex?: number | undefined;
    SubtitleStreamIndex?: number | undefined;
    PlaybackRate: number;
    CanSeek: true;
}

export interface CustomProgressReport {
    ItemId: string;
    MediaSourceId?: string | null | undefined;
    PositionSeconds: number;
    DurationSeconds: number;
    IsPaused: boolean;
    PlaySessionId?: string | null | undefined;
    ClientName: 'Jellyfin Web New';
}

export interface MediaSegment {
    Type?: string | null;
    StartTicks?: number | null;
    EndTicks?: number | null;
}

export interface NextEpisode {
    HasNext: boolean;
    DelaySeconds: number;
    Item?: {
        Id?: string | null;
        Name?: string | null;
    } | null;
    ResumePositionSeconds: number;
    Reason: string;
    RequiresStillWatchingConfirmation: boolean;
}

export interface PlaybackHttpClient {
    /**
     * Authenticated JSON request. `path` is relative to Jellyfin's API root.
     */
    request<T>(path: string, init?: RequestInit): Promise<T>;
    /**
     * Absolute API URL. Authentication must remain in headers, never this URL.
     */
    url(path: string, query?: Record<string, boolean | number | string | null | undefined>): string;
    /**
     * Authentication headers used by HLS and final keepalive reports.
     */
    authHeaders(): HeadersInit;
    /**
     * Media URL for a native media element. A token may be added here only when
     * the server cannot authenticate media through same-origin credentials.
     */
    mediaUrl(path: string, query?: Record<string, boolean | number | string | null | undefined>): string;
}

export interface PlayerItem {
    Id: string;
    Name?: string | null;
    RunTimeTicks?: number | null;
    Type?: string | null;
}

export interface PlayerSnapshot {
    status: 'idle' | 'loading' | 'playing' | 'paused' | 'ended' | 'error';
    item?: PlayerItem | undefined;
    profileId?: string | undefined;
    selection?: PlaybackSelection | undefined;
    playSessionId?: string | null | undefined;
    currentTime: number;
    duration: number;
    volume: number;
    playbackRate: number;
    error?: PlaybackErrorCode | undefined;
}
