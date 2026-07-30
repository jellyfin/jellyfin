import {
    useMutation,
    useQuery,
    useQueryClient
} from '@tanstack/react-query';
import {
    createContext,
    type FormEvent,
    type PropsWithChildren,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState
} from 'react';
import { Link, useNavigate } from 'react-router-dom';

import {
    ApiError,
    profileQueryKeys,
    profilesApi,
    type Profile
} from '../../api';
import { useOnlineStatus } from '../../app/useOnlineStatus';
import { useSession } from '../../auth';
import styles from './ProfilesPages.module.css';

type Translate = (
    key: string,
    values?: Record<string, number | string>
) => string;

const avatars = ['stone', 'snow', 'ocean', 'forest', 'amber'] as const;

type ProfileAction =
    | { avatarId: string; kind: 'create'; name: string }
    | { avatarId: string; kind: 'update'; name: string; profileId: string }
    | { kind: 'delete'; profileId: string };

interface ProfileContextValue {
    activeProfile: Profile | null;
    isLoading: boolean;
    selectProfile: (profileId: string) => Promise<void>;
}

const ProfileContext = createContext<ProfileContextValue | null>(null);

function useProfilesQuery() {
    const { client, session, status } = useSession();
    const online = useOnlineStatus();
    const userId = session?.userId || 'anonymous';
    return useQuery({
        enabled: status === 'authenticated' && online,
        queryFn: () => profilesApi.getAll(client),
        queryKey: profileQueryKeys.profiles(userId)
    });
}

export function ProfileProvider({ children }: PropsWithChildren) {
    const { client, rememberProfile, session, status } = useSession();
    const online = useOnlineStatus();
    const queryClient = useQueryClient();
    const userId = session?.userId || 'anonymous';
    const active = useQuery({
        enabled: status === 'authenticated' && online,
        initialData: session?.activeProfile
            ? { Profile: session.activeProfile, ProfileId: session.activeProfile.Id }
            : undefined,
        queryFn: () => profilesApi.getActive(client),
        queryKey: profileQueryKeys.active(userId),
        retry: false
    });

    const selectProfile = useCallback(async (profileId: string) => {
        if (!navigator.onLine) throw new ApiError('network');
        const selected = await profilesApi.setActive(client, profileId);
        if (selected.Profile) rememberProfile(selected.Profile);
        queryClient.removeQueries({
            predicate: ({ queryKey }) =>
                (queryKey[0] === 'custom-netflix'
                    && queryKey[1] === userId
                    && queryKey[2] !== 'all')
                || (queryKey[0] === 'catalog' && queryKey[1] === userId)
        });
        queryClient.setQueryData(profileQueryKeys.active(userId), selected);
    }, [client, queryClient, rememberProfile, userId]);

    useEffect(() => {
        if (active.data?.Profile) rememberProfile(active.data.Profile);
    }, [active.data?.Profile, rememberProfile]);

    const value = useMemo<ProfileContextValue>(() => ({
        activeProfile: active.data?.Profile ?? null,
        isLoading: active.isLoading,
        selectProfile
    }), [active.data?.Profile, active.isLoading, selectProfile]);

    return <ProfileContext.Provider value={value}>{children}</ProfileContext.Provider>;
}

export function useProfile(): ProfileContextValue {
    const context = useContext(ProfileContext);
    if (!context) throw new Error('useProfile must be used inside ProfileProvider.');
    return context;
}

function profileError(error: unknown, t: Translate): string {
    if (error instanceof ApiError && error.status === 409) {
        return t('profiles.playbackConflict');
    }
    if (error instanceof ApiError && error.status === 503) {
        return t('errors.unavailable');
    }
    return t('errors.unknown');
}

export function ProfileAvatar({
    avatarId,
    name
}: {
    avatarId?: string | null;
    name: string;
}) {
    return (
        <span aria-hidden="true" className={styles.avatar} data-avatar={avatarId || 'stone'}>
            {name.trim().slice(0, 1).toLocaleUpperCase()}
        </span>
    );
}

export function ProfilesPage({ t }: { t: Translate }) {
    const online = useOnlineStatus();
    const { selectProfile } = useProfile();
    const profiles = useProfilesQuery();
    const navigate = useNavigate();
    const [error, setError] = useState('');
    const [pending, setPending] = useState('');

    async function select(profileId: string) {
        setError('');
        setPending(profileId);
        try {
            await selectProfile(profileId);
            navigate('/home', { replace: true });
        } catch (cause) {
            setError(profileError(cause, t));
        } finally {
            setPending('');
        }
    }

    return (
        <main className={styles.page}>
            <header className={styles.header}>
                <span className={styles.brand}>JELLYFIN<span>VIEW</span></span>
                <Link to="/profiles/manage">{t('profiles.manage')}</Link>
            </header>
            <section className={styles.selector}>
                <p className={styles.kicker}>{t('profiles.kicker')}</p>
                <h1>{t('profiles.choose')}</h1>
                {profiles.isLoading && <p aria-live="polite">{t('actions.loading')}</p>}
                {profiles.isError && <p className={styles.error}>{profileError(profiles.error, t)}</p>}
                <div className={styles.grid}>
                    {profiles.data?.Profiles.map(profile => (
                        <button
                            className={styles.profileButton}
                            disabled={!online || Boolean(pending)}
                            key={profile.Id}
                            onClick={() => void select(profile.Id)}
                            type="button"
                        >
                            <ProfileAvatar avatarId={profile.AvatarId ?? null} name={profile.Name} />
                            <span>{profile.Name}</span>
                            {pending === profile.Id && <small>{t('actions.loading')}</small>}
                        </button>
                    ))}
                </div>
                <p aria-live="assertive" className={styles.error}>{error}</p>
            </section>
        </main>
    );
}

export function ManageProfilesPage({ t }: { t: Translate }) {
    const online = useOnlineStatus();
    const { client, session } = useSession();
    const profiles = useProfilesQuery();
    const queryClient = useQueryClient();
    const userId = session?.userId || 'anonymous';
    const [error, setError] = useState('');

    const mutation = useMutation<void, Error, ProfileAction>({
        mutationFn: async (action) => {
            if (!navigator.onLine) throw new ApiError('network');
            if (action.kind === 'create') {
                await profilesApi.create(client, {
                    AvatarId: action.avatarId,
                    Name: action.name
                });
                return;
            }
            if (action.kind === 'update') {
                await profilesApi.update(client, action.profileId, {
                    AvatarId: action.avatarId,
                    Name: action.name
                });
                return;
            }
            await profilesApi.delete(client, action.profileId);
        },
        onError: cause => setError(profileError(cause, t)),
        onSuccess: async () => {
            setError('');
            await queryClient.invalidateQueries({
                queryKey: profileQueryKeys.profiles(userId)
            });
            await queryClient.invalidateQueries({
                queryKey: profileQueryKeys.active(userId)
            });
        }
    });

    function create(event: FormEvent<HTMLFormElement>) {
        event.preventDefault();
        const form = event.currentTarget;
        const data = new FormData(form);
        mutation.mutate({
            avatarId: String(data.get('avatar') || avatars[0]),
            kind: 'create',
            name: String(data.get('name') || '').trim()
        }, {
            onSuccess: () => form.reset()
        });
    }

    function update(event: FormEvent<HTMLFormElement>, profileId: string) {
        event.preventDefault();
        const data = new FormData(event.currentTarget);
        mutation.mutate({
            avatarId: String(data.get('avatar') || avatars[0]),
            kind: 'update',
            name: String(data.get('name') || '').trim(),
            profileId
        });
    }

    function remove(profile: Profile) {
        if (window.confirm(t('profiles.deleteConfirm', { name: profile.Name }))) {
            mutation.mutate({ kind: 'delete', profileId: profile.Id });
        }
    }

    const allProfiles = profiles.data?.Profiles ?? [];
    const canCreate = allProfiles.length < 5;

    return (
        <main className={styles.managePage}>
            <header className={styles.manageHeader}>
                <div>
                    <p className={styles.kicker}>{t('profiles.manageKicker')}</p>
                    <h1>{t('profiles.manage')}</h1>
                </div>
                <Link className={styles.done} to="/profiles">{t('actions.done')}</Link>
            </header>

            <p aria-live="assertive" className={styles.error}>{error}</p>
            <div className={styles.editorList}>
                {allProfiles.map(profile => (
                    <form
                        className={styles.editor}
                        key={profile.Id}
                        onSubmit={event => update(event, profile.Id)}
                    >
                        <ProfileAvatar avatarId={profile.AvatarId ?? null} name={profile.Name} />
                        <label>
                            <span>{t('profiles.name')}</span>
                            <input defaultValue={profile.Name} maxLength={32} name="name" required />
                        </label>
                        <AvatarSelect defaultValue={profile.AvatarId || avatars[0]} t={t} />
                        <div className={styles.editorActions}>
                            <button disabled={!online || mutation.isPending} type="submit">{t('actions.save')}</button>
                            <button
                                disabled={!online || mutation.isPending || allProfiles.length <= 1}
                                onClick={() => remove(profile)}
                                type="button"
                            >
                                {t('actions.delete')}
                            </button>
                        </div>
                    </form>
                ))}
            </div>

            {canCreate && (
                <form className={styles.create} onSubmit={create}>
                    <h2>{t('profiles.add')}</h2>
                    <label>
                        <span>{t('profiles.name')}</span>
                        <input maxLength={32} name="name" required />
                    </label>
                    <AvatarSelect defaultValue={avatars[0]} t={t} />
                    <button className={styles.done} disabled={!online || mutation.isPending} type="submit">
                        {t('profiles.create')}
                    </button>
                </form>
            )}
            {!canCreate && <p className={styles.limit}>{t('profiles.limit', { count: 5 })}</p>}
        </main>
    );
}

function AvatarSelect({
    defaultValue,
    t
}: {
    defaultValue: string;
    t: Translate;
}) {
    return (
        <label>
            <span>{t('profiles.avatar')}</span>
            <select defaultValue={defaultValue} name="avatar">
                {avatars.map(avatar => (
                    <option key={avatar} value={avatar}>
                        {t(`profiles.avatars.${avatar}`)}
                    </option>
                ))}
            </select>
        </label>
    );
}
