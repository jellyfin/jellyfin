import type { JellyfinClient } from './client';

export interface ProfileSettings {
    AutoplayDelaySeconds: number;
    AutoplayEnabled: boolean;
    SkipIntroEnabled: boolean;
    SkipRecapEnabled: boolean;
}

export interface PlaybackPreferences {
    AllowAudioTranscoding: boolean;
    AllowContainerRemuxing: boolean;
    AllowVideoTranscoding: boolean;
    AudioDescriptionEnabled: boolean;
    ClosedCaptionsEnabled: boolean;
    MaxStreamingBitrate?: number | null;
    PreferDirectPlay: boolean;
    PreferHardwareTranscoding: boolean;
    PreferredAudioLanguage?: string | null;
    PreferredSubtitleLanguage?: string | null;
    SkipCreditsEnabled: boolean;
    SubtitlesEnabled: boolean;
}

export interface Profile {
    AvatarId?: string | null;
    CreatedAt: string;
    Id: string;
    IsChild: boolean;
    IsDefault: boolean;
    JellyfinUserId: string;
    Name: string;
    PlaybackPreferences: PlaybackPreferences;
    Settings: ProfileSettings;
    UpdatedAt: string;
}

export interface ProfilesResponse {
    Profiles: Profile[];
}

export interface ActiveProfile {
    Profile?: Profile | null;
    ProfileId: string;
}

export interface CreateProfile {
    AvatarId?: string | null;
    Name: string;
}

export interface UpdateProfile {
    AvatarId?: string | null;
    Name?: string;
    PlaybackPreferences?: PlaybackPreferences;
    Settings?: ProfileSettings;
}

export const profilesApi = {
    getAll: (client: JellyfinClient) =>
        client.request<ProfilesResponse>('GET', 'CustomNetflix/v1/profiles'),
    create: (client: JellyfinClient, profile: CreateProfile) =>
        client.request<Profile>('POST', 'CustomNetflix/v1/profiles', {
            body: { ...profile, IsChild: false }
        }),
    update: (client: JellyfinClient, profileId: string, profile: UpdateProfile) =>
        client.request<Profile>('PATCH', `CustomNetflix/v1/profiles/${profileId}`, {
            body: profile
        }),
    delete: (client: JellyfinClient, profileId: string) =>
        client.request<void>('DELETE', `CustomNetflix/v1/profiles/${profileId}`),
    getActive: (client: JellyfinClient) =>
        client.request<ActiveProfile>('GET', 'CustomNetflix/v1/profiles/active'),
    setActive: (client: JellyfinClient, profileId: string) =>
        client.request<ActiveProfile>('PUT', 'CustomNetflix/v1/profiles/active', {
            body: { ProfileId: profileId }
        })
};
