import { Navigate, Outlet, createHashRouter } from 'react-router-dom';

import { useSession } from '../auth';
import { AppShell } from '../components/AppShell';
import { OfflineBanner } from '../components/OfflineBanner';
import {
    ForgotPasswordPage,
    LoginPage,
    RegisterPage,
    ServerUnavailablePage,
    SetupRequiredPage
} from '../features/auth';
import {
    ManageProfilesPage,
    ProfileProvider,
    ProfilesPage
} from '../features/profiles';
import { useI18n } from '../i18n';
import { LoadingPage, NotFoundPage, RouteErrorPage } from '../pages/SystemPages';
import { RuntimeOutlet } from './runtime';

function AnonymousGate() {
    const { status } = useSession();
    const { t } = useI18n();
    if (status === 'loading') return <LoadingPage />;
    if (status === 'setup-required') return <SetupRequiredPage t={t} />;
    if (status === 'unavailable') return <ServerUnavailablePage t={t} />;
    if (status === 'authenticated') return <Navigate replace to='/profiles' />;
    return <Outlet />;
}

function SessionGate() {
    const { status } = useSession();
    const { t } = useI18n();
    if (status === 'loading') return <LoadingPage />;
    if (status === 'setup-required') return <SetupRequiredPage t={t} />;
    if (status === 'unavailable') return <ServerUnavailablePage t={t} />;
    if (status !== 'authenticated') return <Navigate replace to='/login' />;
    return <ProfileProvider><Outlet /></ProfileProvider>;
}

function LoginRoute() {
    const { t } = useI18n();
    return <LoginPage t={t} />;
}

function RegisterRoute() {
    const { t } = useI18n();
    return <RegisterPage t={t} />;
}

function ForgotRoute() {
    const { t } = useI18n();
    return <ForgotPasswordPage t={t} />;
}

function ProfilesRoute() {
    const { t } = useI18n();
    return <ProfilesPage t={t} />;
}

function ManageProfilesRoute() {
    const { t } = useI18n();
    return <ManageProfilesPage t={t} />;
}

function ViewerChrome() {
    return (
        <>
            <OfflineBanner />
            <AppShell />
        </>
    );
}

export const router = createHashRouter([
    {
        errorElement: <RouteErrorPage />,
        hydrateFallbackElement: <LoadingPage />,
        path: '/',
        children: [
            { index: true, element: <Navigate replace to='/home' /> },
            {
                element: <AnonymousGate />,
                children: [
                    { path: 'login', element: <LoginRoute /> },
                    { path: 'register', element: <RegisterRoute /> },
                    { path: 'forgot-password', element: <ForgotRoute /> }
                ]
            },
            {
                element: <SessionGate />,
                children: [
                    { path: 'profiles', element: <ProfilesRoute /> },
                    { path: 'profiles/manage', element: <ManageProfilesRoute /> },
                    {
                        element: <RuntimeOutlet />,
                        children: [
                            {
                                element: <ViewerChrome />,
                                children: [
                                    {
                                        path: 'home',
                                        lazy: async () => {
                                            const { HomePage } = await import('../features/home/HomePage');
                                            return { Component: HomePage };
                                        }
                                    },
                                    {
                                        path: 'browse/movies',
                                        lazy: async () => {
                                            const { BrowsePage } = await import('../features/browse/BrowsePage');
                                            return { Component: () => <BrowsePage type='Movie' /> };
                                        }
                                    },
                                    {
                                        path: 'browse/series',
                                        lazy: async () => {
                                            const { BrowsePage } = await import('../features/browse/BrowsePage');
                                            return { Component: () => <BrowsePage type='Series' /> };
                                        }
                                    },
                                    {
                                        path: 'new-and-popular',
                                        lazy: async () => {
                                            const { NewAndPopularPage } = await import('../features/browse/NewAndPopularPage');
                                            return { Component: NewAndPopularPage };
                                        }
                                    },
                                    {
                                        path: 'search',
                                        lazy: async () => {
                                            const { SearchPage } = await import('../features/search/SearchPage');
                                            return { Component: SearchPage };
                                        }
                                    },
                                    {
                                        path: 'title/:itemId',
                                        lazy: async () => {
                                            const { TitlePage } = await import('../features/title/TitlePage');
                                            return { Component: TitlePage };
                                        }
                                    },
                                    {
                                        path: 'my-list',
                                        lazy: async () => {
                                            const { MyListPage } = await import('../features/my-list/MyListPage');
                                            return { Component: MyListPage };
                                        }
                                    },
                                    {
                                        path: 'history',
                                        lazy: async () => {
                                            const { HistoryPage } = await import('../features/history/HistoryPage');
                                            return { Component: HistoryPage };
                                        }
                                    },
                                    {
                                        path: 'settings/profile',
                                        lazy: async () => {
                                            const { ProfileSettingsPage } = await import('../pages/SettingsPages');
                                            return { Component: ProfileSettingsPage };
                                        }
                                    },
                                    {
                                        path: 'settings/playback',
                                        lazy: async () => {
                                            const { PlaybackSettingsPage } = await import('../pages/SettingsPages');
                                            return { Component: PlaybackSettingsPage };
                                        }
                                    },
                                    {
                                        path: 'account',
                                        lazy: async () => {
                                            const { AccountPage } = await import('../pages/SettingsPages');
                                            return { Component: AccountPage };
                                        }
                                    }
                                ]
                            },
                            {
                                path: 'watch/:itemId',
                                lazy: async () => {
                                    const { WatchPage } = await import('../pages/WatchPage');
                                    return { Component: WatchPage };
                                }
                            }
                        ]
                    }
                ]
            },
            { path: '*', element: <NotFoundPage /> }
        ]
    }
]);
