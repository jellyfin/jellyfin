import { Link } from 'react-router-dom';

import type { MediaSummary } from '../types/media';
import styles from './MediaCard.module.css';

interface MediaCardProps {
    item: MediaSummary;
    number?: number;
    variant?: 'landscape' | 'poster';
}

export function MediaCard({ item, number, variant = 'poster' }: MediaCardProps) {
    const label = item.seriesName
        ? `${item.seriesName} — ${item.name}`
        : item.name;
    const imageUrl = variant === 'landscape'
        ? item.backdropUrl ?? item.imageUrl
        : item.imageUrl ?? item.backdropUrl;

    return (
        <article className={`${styles.card} ${styles[variant]}`}>
            {number ? <span className={styles.number} aria-hidden='true'>{number.toString().padStart(2, '0')}</span> : null}
            <Link className={styles.link} to={`/title/${item.id}`} aria-label={label} viewTransition>
                <div className={styles.artwork}>
                    {imageUrl
                        ? <img alt='' loading='lazy' decoding='async' src={imageUrl} />
                        : <span className={styles.placeholder} aria-hidden='true'>{item.name.slice(0, 1)}</span>}
                    {item.progressPercent && item.progressPercent > 0 ? (
                        <span className={styles.progress} aria-label={`${Math.round(item.progressPercent)} %`}>
                            <span style={{ width: `${Math.min(100, item.progressPercent)}%` }} />
                        </span>
                    ) : null}
                </div>
                <span className={styles.title}>{label}</span>
            </Link>
        </article>
    );
}
