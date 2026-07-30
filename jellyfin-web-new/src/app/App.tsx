import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { useEffect, useState } from 'react';
import { RouterProvider } from 'react-router-dom';

import { SessionProvider } from '../auth';
import { Button } from '../components/Button';
import { I18nProvider, useI18n } from '../i18n';
import { clearMetadataSnapshots, registerPwa, type InstallUpdate } from '../pwa';
import { playerStore } from '../player';
import { router } from './router';
import styles from './App.module.css';

const queryClient = new QueryClient({
    defaultOptions: {
        mutations: { retry: false },
        queries: {
            gcTime: 30 * 60_000,
            refetchOnWindowFocus: false,
            retry(failureCount, error) {
                const status = (error as { status?: number }).status;
                return failureCount < 2 && status !== 401 && status !== 403 && status !== 404 && status !== 409;
            },
            staleTime: 30_000
        }
    }
});

async function purgeClientState() {
    queryClient.clear();
    playerStore.reset();
    await clearMetadataSnapshots();
    if ('caches' in window) {
        const names = await caches.keys();
        await Promise.all(
            names
                .filter(name => name.startsWith('jellyfin-web-new-'))
                .map(name => caches.delete(name))
        );
    }
}

function UpdatePrompt() {
    const [ install, setInstall ] = useState<InstallUpdate>();
    const { locale } = useI18n();

    useEffect(() => {
        let dispose: (() => void) | undefined;
        void registerPwa(nextInstall => setInstall(() => nextInstall)).then(value => {
            dispose = value;
        });
        return () => dispose?.();
    }, []);

    if (!install) return null;
    return (
        <aside className={styles.update} role='status'>
            <p>{locale === 'fr' ? 'Une nouvelle version est prête.' : 'A new version is ready.'}</p>
            <Button
                onClick={() => {
                    void install();
                    setInstall(undefined);
                }}
                tone='primary'
            >
                {locale === 'fr' ? 'Mettre à jour' : 'Update'}
            </Button>
        </aside>
    );
}

export function App() {
    return (
        <QueryClientProvider client={queryClient}>
            <I18nProvider>
                <SessionProvider purgeClientState={purgeClientState}>
                    <RouterProvider router={router} />
                    <UpdatePrompt />
                </SessionProvider>
            </I18nProvider>
        </QueryClientProvider>
    );
}
