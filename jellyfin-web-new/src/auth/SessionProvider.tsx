import type { AuthenticationResult, ForgotPasswordResult } from '@jellyfin/sdk/lib/generated-client';
import {
    createContext,
    type PropsWithChildren,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState
} from 'react';

import {
    ApiError,
    createJellyfinClient,
    getDefaultApiBaseUrl,
    type JellyfinClient,
    type Profile,
    type PublicSystemInfo
} from '../api';
import { SessionStorage, type StoredSession } from './storage';

export type SessionStatus =
    | 'anonymous'
    | 'authenticated'
    | 'loading'
    | 'setup-required'
    | 'unavailable';

export interface SessionContextValue {
    client: JellyfinClient;
    error: ApiError | null;
    forgotPassword: (username: string) => Promise<ForgotPasswordResult>;
    login: (username: string, password: string) => Promise<void>;
    logout: () => Promise<void>;
    publicInfo: PublicSystemInfo | null;
    rememberProfile: (profile: Profile) => void;
    register: (username: string, password: string) => Promise<void>;
    session: StoredSession | null;
    status: SessionStatus;
}

interface SessionProviderProps extends PropsWithChildren {
    baseUrl?: string;
    client?: JellyfinClient;
    localStorage?: Storage;
    purgeClientState?: () => Promise<void> | void;
}

const SessionContext = createContext<SessionContextValue | null>(null);

function sessionFrom(result: AuthenticationResult): StoredSession {
    const accessToken = result.AccessToken;
    const userId = result.User?.Id;
    if (!accessToken || !userId) {
        throw new ApiError('unknown', undefined, undefined, 'Incomplete authentication response.');
    }

    return {
        accessToken,
        ...(result.ServerId ? { serverId: result.ServerId } : {}),
        ...(result.SessionInfo?.Id ? { sessionId: result.SessionInfo.Id } : {}),
        userId
    };
}

export function SessionProvider({
    baseUrl = getDefaultApiBaseUrl(),
    children,
    client: suppliedClient,
    localStorage,
    purgeClientState
}: SessionProviderProps) {
    const sessionStorage = useMemo(
        () => new SessionStorage(baseUrl, localStorage),
        [baseUrl, localStorage]
    );
    const initialSession = useMemo(() => sessionStorage.getSession(), [sessionStorage]);
    const client = useMemo(
        () => suppliedClient ?? createJellyfinClient(
            sessionStorage.getDeviceId(),
            initialSession?.accessToken,
            baseUrl
        ),
        [baseUrl, initialSession?.accessToken, sessionStorage, suppliedClient]
    );
    const [error, setError] = useState<ApiError | null>(null);
    const [publicInfo, setPublicInfo] = useState<PublicSystemInfo | null>(null);
    const [session, setSession] = useState<StoredSession | null>(initialSession);
    const [status, setStatus] = useState<SessionStatus>('loading');

    useEffect(() => {
        let active = true;

        void (async () => {
            try {
                const info = await client.getPublicSystemInfo();
                if (!active) return;
                setPublicInfo(info);

                if (info.StartupWizardCompleted === false) {
                    setStatus('setup-required');
                    return;
                }

                if (!initialSession) {
                    setStatus('anonymous');
                    return;
                }

                await client.getCurrentUser();
                if (active) setStatus('authenticated');
            } catch (cause) {
                if (!active) return;
                const apiError = cause instanceof ApiError ? cause : new ApiError('unknown');
                if (initialSession && !navigator.onLine) {
                    setStatus('authenticated');
                    return;
                }
                if (initialSession && apiError.code === 'unauthorized') {
                    sessionStorage.clearSession();
                    client.setAccessToken();
                    setSession(null);
                    setStatus('anonymous');
                } else {
                    setError(apiError);
                    setStatus('unavailable');
                }
            }
        })();

        return () => {
            active = false;
        };
    }, [client, initialSession, sessionStorage]);

    const applyAuthentication = useCallback((result: AuthenticationResult) => {
        const nextSession = sessionFrom(result);
        sessionStorage.setSession(nextSession);
        client.setAccessToken(nextSession.accessToken);
        setSession(nextSession);
        setError(null);
        setStatus('authenticated');
    }, [client, sessionStorage]);

    const login = useCallback(async (username: string, password: string) => {
        applyAuthentication(await client.authenticate(username, password));
    }, [applyAuthentication, client]);

    const register = useCallback(async (username: string, password: string) => {
        applyAuthentication(await client.register(username, password));
    }, [applyAuthentication, client]);

    const rememberProfile = useCallback((profile: Profile) => {
        setSession(current => {
            if (!current) return current;
            const next = { ...current, activeProfile: profile };
            sessionStorage.setSession(next);
            return next;
        });
    }, [sessionStorage]);

    const forgotPassword = useCallback(
        (username: string) => client.forgotPassword(username),
        [client]
    );

    const logout = useCallback(async () => {
        await client.reportSessionEnded();
        sessionStorage.clearSession();
        client.setAccessToken();
        setSession(null);
        setStatus('anonymous');
        await purgeClientState?.();
    }, [client, purgeClientState, sessionStorage]);

    const value = useMemo<SessionContextValue>(() => ({
        client,
        error,
        forgotPassword,
        login,
        logout,
        publicInfo,
        rememberProfile,
        register,
        session,
        status
    }), [
        client,
        error,
        forgotPassword,
        login,
        logout,
        publicInfo,
        rememberProfile,
        register,
        session,
        status
    ]);

    return <SessionContext.Provider value={value}>{children}</SessionContext.Provider>;
}

export function useSession(): SessionContextValue {
    const context = useContext(SessionContext);
    if (!context) throw new Error('useSession must be used inside SessionProvider.');
    return context;
}
