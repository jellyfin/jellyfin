import { Button } from './Button';
import styles from './StatusView.module.css';
import { useI18n } from '../i18n';

interface StatusViewProps {
    detail?: string;
    title?: string;
    action?: () => void;
}

export function StatusView({ action, detail, title }: StatusViewProps) {
    const { t } = useI18n();

    return (
        <section className={styles.view}>
            <span aria-hidden='true'>×</span>
            <h1>{title ?? t('error')}</h1>
            <p>{detail ?? t('errorDescription')}</p>
            {action ? <Button onClick={action}>{t('retry')}</Button> : null}
        </section>
    );
}
