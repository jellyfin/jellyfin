import { useOnlineStatus } from '../app/useOnlineStatus';
import { useI18n } from '../i18n';
import styles from './OfflineBanner.module.css';

export function OfflineBanner() {
    const online = useOnlineStatus();
    const { t } = useI18n();

    return online ? null : <p className={styles.banner} role='status'>{t('offline')}</p>;
}
