import type { Page, Route } from '@playwright/test';

export const profile = {
    AvatarId: 'ocean',
    CreatedAt: '2026-07-30T00:00:00Z',
    Id: 'profile-1',
    IsChild: false,
    IsDefault: true,
    JellyfinUserId: 'user-1',
    Name: 'Master',
    PlaybackPreferences: {
        AllowAudioTranscoding: true,
        AllowContainerRemuxing: true,
        AllowVideoTranscoding: true,
        AudioDescriptionEnabled: false,
        ClosedCaptionsEnabled: false,
        MaxStreamingBitrate: 20_000_000,
        PreferDirectPlay: true,
        PreferHardwareTranscoding: true,
        PreferredAudioLanguage: 'fra',
        PreferredSubtitleLanguage: 'eng',
        SkipCreditsEnabled: true,
        SubtitlesEnabled: true
    },
    Settings: {
        AutoplayDelaySeconds: 8,
        AutoplayEnabled: true,
        SkipIntroEnabled: true,
        SkipRecapEnabled: true
    },
    UpdatedAt: '2026-07-30T00:00:00Z'
};

const items = [
    {
        BackdropImageTags: [ 'backdrop-aurora' ],
        CommunityRating: 8.4,
        Genres: [ 'Science fiction', 'Drama' ],
        Id: 'item-aurora',
        ImageTags: { Primary: 'poster-aurora' },
        Name: 'Aurora',
        OfficialRating: '12',
        Overview: 'A cartographer follows a signal beyond the last mapped light.',
        ProductionYear: 2026,
        RunTimeTicks: 7_680_000_000,
        Type: 'Movie'
    },
    {
        BackdropImageTags: [ 'backdrop-signal' ],
        CommunityRating: 7.9,
        Id: 'item-signal',
        ImageTags: { Primary: 'poster-signal' },
        Name: 'Signal Zero',
        Overview: 'A silent transmission reaches Earth.',
        ProductionYear: 2025,
        RunTimeTicks: 6_600_000_000,
        Type: 'Movie'
    },
    {
        BackdropImageTags: [ 'backdrop-north' ],
        CommunityRating: 8.1,
        Id: 'item-north',
        ImageTags: { Primary: 'poster-north' },
        Name: 'Northbound',
        Overview: 'One train, no final station.',
        ProductionYear: 2024,
        RunTimeTicks: 5_400_000_000,
        Type: 'Series'
    }
];

const progress = {
    Completed: false,
    DurationSeconds: 7680,
    ItemId: 'item-aurora',
    LastPlayedAt: '2026-07-30T12:00:00Z',
    PercentViewed: 37,
    PlayCount: 1,
    PositionSeconds: 2841,
    ProfileId: profile.Id
};

function json(route: Route, body: unknown, status = 200) {
    return route.fulfill({
        body: JSON.stringify(body),
        contentType: 'application/json',
        status
    });
}

function artwork(route: Route) {
    const id = new URL(route.request().url()).pathname;
    const hue = id.includes('signal') ? 28 : id.includes('north') ? 178 : 216;
    return route.fulfill({
        body: `<svg xmlns="http://www.w3.org/2000/svg" width="1280" height="720"><defs><linearGradient id="g"><stop stop-color="hsl(${hue} 74% 56%)"/><stop offset="1" stop-color="hsl(${hue + 45} 75% 12%)"/></linearGradient></defs><rect width="1280" height="720" fill="url(#g)"/><circle cx="900" cy="250" r="190" fill="white" opacity=".16"/></svg>`,
        contentType: 'image/svg+xml'
    });
}

export async function mockJellyfin(page: Page, registration = true) {
    await page.route('**/jellyfin-api/**', async route => {
        const request = route.request();
        const url = new URL(request.url());
        const path = url.pathname.replace(/^.*\/jellyfin-api\//, '');
        const method = request.method();

        if (/\/Images\//.test(path)) return artwork(route);
        if (path === 'System/Info/Public') {
            return json(route, {
                EnablePublicUserRegistration: registration,
                PublicUserRegistrationMinimumPasswordLength: 10,
                ServerName: 'Orion',
                StartupWizardCompleted: true,
                Version: '10.11.0'
            });
        }
        if (path === 'Users/AuthenticateByName' && method === 'POST') {
            return json(route, {
                AccessToken: 'test-token',
                ServerId: 'server-1',
                SessionInfo: { Id: 'session-1' },
                User: { Id: 'user-1', Name: 'master' }
            });
        }
        if (path === 'Users/Me') return json(route, { Id: 'user-1', Name: 'master' });
        if (path === 'Users/ForgotPassword') return json(route, { Action: 'ContactAdmin' });
        if (path === 'Users/Register') {
            return json(route, {
                AccessToken: 'test-token',
                ServerId: 'server-1',
                SessionInfo: { Id: 'session-1' },
                User: { Id: 'user-1', Name: 'master' }
            });
        }
        if (path === 'Sessions/Logout') return json(route, {});
        if (path === 'CustomNetflix/v1/profiles/active') {
            return json(route, { Profile: profile, ProfileId: profile.Id });
        }
        if (path === 'CustomNetflix/v1/profiles' && method === 'GET') {
            return json(route, { Profiles: [ profile ] });
        }
        if (path === 'CustomNetflix/v1/home') {
            return json(route, {
                GeneratedAt: '2026-07-30T12:00:00Z',
                ProfileId: profile.Id,
                Rows: [
                    { Id: 'recommended-for-you', Items: [{ Item: items[0], RecommendationReason: 'Because you watched science fiction' }], Title: 'For you' },
                    { Id: 'continue-watching', Items: [{ Item: items[0], Progress: progress }], Title: 'Continue watching' },
                    { Id: 'top10', Items: items.map(Item => ({ Item })), Title: 'Top 10' },
                    { Id: 'trending', Items: items.map(Item => ({ Item })), Title: 'Trending' },
                    { Id: 'new', Items: [ { Item: items[1] }, { Item: items[2] } ], Title: 'New releases' }
                ]
            });
        }
        if (/^CustomNetflix\/v1\/items\/[^/]+\/details$/.test(path)) {
            return json(route, {
                GeneratedAt: '2026-07-30T12:00:00Z',
                Item: items.find(item => item.Id === path.split('/')[3]) ?? items[0],
                ProfileId: profile.Id,
                Progress: path.includes('aurora') ? progress : null
            });
        }
        if (/\/my-list\/[^/]+$/.test(path)) {
            return json(route, {
                AddedAt: null,
                IsInMyList: false,
                ItemId: path.split('/').at(-1),
                ProfileId: profile.Id
            });
        }
        if (/\/feedback$/.test(path)) return json(route, { Feedback: null });
        if (/\/progress\/batch$/.test(path)) return json(route, { Items: [] });
        if (/\/history$/.test(path)) return json(route, { Items: [] });
        if (/\/my-list$/.test(path)) return json(route, { Items: [], ProfileId: profile.Id });
        if (path === 'Genres') return json(route, { Items: [ { Name: 'Drama' }, { Name: 'Science fiction' } ] });
        if (/^Users\/[^/]+\/Items$/.test(path)) {
            return json(route, { Items: items, StartIndex: 0, TotalRecordCount: items.length });
        }
        if (/\/PlaybackInfo$/.test(path)) {
            return json(route, {
                MediaSources: [],
                PlaySessionId: 'play-1'
            });
        }
        if (/^MediaSegments\//.test(path)) return json(route, { Items: [] });
        if (/\/next-episode$/.test(path)) {
            return json(route, {
                DelaySeconds: 8,
                HasNext: false,
                Reason: '',
                RequiresStillWatchingConfirmation: false,
                ResumePositionSeconds: 0
            });
        }
        if (method !== 'GET') return json(route, {});
        return json(route, {}, 404);
    });
}

export async function installSession(page: Page) {
    await page.addInitScript(() => {
        const baseUrl = `${location.origin}/jellyfin-api`;
        const prefix = `jellyfin-web-new:${encodeURIComponent(baseUrl)}`;
        localStorage.setItem(`${prefix}:device-id`, 'device-1');
        localStorage.setItem(`${prefix}:session`, JSON.stringify({
            accessToken: 'test-token',
            serverId: 'server-1',
            sessionId: 'session-1',
            userId: 'user-1'
        }));
        localStorage.setItem('jellyfin-web-new:locale', 'en');
    });
}
