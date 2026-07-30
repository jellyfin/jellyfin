export const profileQueryKeys = {
    scope: (userId: string, profileId: string) =>
        ['custom-netflix', userId, profileId] as const,
    profiles: (userId: string) =>
        [...profileQueryKeys.scope(userId, 'all'), 'profiles'] as const,
    active: (userId: string) =>
        [...profileQueryKeys.scope(userId, 'active'), 'profile'] as const,
    data: (userId: string, profileId: string, ...parts: readonly unknown[]) =>
        [...profileQueryKeys.scope(userId, profileId), ...parts] as const
};
