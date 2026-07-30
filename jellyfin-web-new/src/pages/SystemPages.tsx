import { Link, useRouteError } from 'react-router-dom';

import { StatusView } from '../components/StatusView';
import { useI18n } from '../i18n';
import styles from './SystemPages.module.css';

export function LoadingPage() {
    const { t } = useI18n();
    return (
        <main className={styles.loading} role='status'>
            <span aria-hidden='true' />
            <p>{t('loading')}</p>
        </main>
    );
}

export function NotFoundPage() {
    const { locale } = useI18n();
    return (
        <main className={styles.notFound}>
            <p>404</p>
            <h1>{locale === 'fr' ? 'Cette page n’existe pas' : 'This page does not exist'}</h1>
            <Link to='/home'>{locale === 'fr' ? 'Retour à l’accueil' : 'Back home'}</Link>
        </main>
    );
}

export function RouteErrorPage() {
    const error = useRouteError();
    const detail = error instanceof Error ? error.message : undefined;
    return <StatusView {...(detail ? { detail } : {})} />;
}
