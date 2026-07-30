import {
    createContext,
    type PropsWithChildren,
    type ReactNode,
    useContext
} from 'react';
import { Link } from 'react-router-dom';

import styles from './catalog.module.css';

export type MediaType = 'Episode' | 'Movie' | 'Season' | 'Series';
export type ImageKind = 'Backdrop' | 'Primary';

export interface MediaItem {
    Id?: string;
    Name?: string;
    Type?: MediaType | string;
    Overview?: string;
    ProductionYear?: number;
    PremiereDate?: string;
    RunTimeTicks?: number;
    OfficialRating?: string;
    CommunityRating?: number;
    Genres?: string[];
    SeriesName?: string;
    IndexNumber?: number;
    ParentIndexNumber?: number;
    ParentId?: string;
    ImageTags?: Record<string, string>;
    BackdropImageTags?: string[];
    ParentBackdropItemId?: string;
    ParentBackdropImageTags?: string[];
    People?: Array<{ Id?: string; Name?: string; Role?: string; Type?: string }>;
}

export interface WatchProgress {
    ProfileId: string;
    ItemId: string;
    PositionSeconds: number;
    DurationSeconds: number;
    PercentViewed: number;
    Completed: boolean;
    PlayCount: number;
    LastPlayedAt: string;
}

export interface CatalogRequest {
    method?: 'DELETE' | 'GET' | 'POST' | 'PUT';
    body?: unknown;
    signal?: AbortSignal;
}

/**
 * Deliberately structural: the authenticated SDK adapter owns auth and HTTP,
 * while catalogue features only describe the request they need.
 */
export interface CatalogClient {
    request<T>(path: string, request?: CatalogRequest): Promise<T>;
    imageUrl(item: MediaItem, kind: ImageKind, width: number): string | undefined;
}

export type CatalogLocale = 'en' | 'fr';

export interface CatalogSession {
    client: CatalogClient;
    locale: CatalogLocale;
    profileId: string;
    userId: string;
}

const CatalogContext = createContext<CatalogSession | null>(null);

export function CatalogProvider({
    children,
    value
}: PropsWithChildren<{ value: CatalogSession }>) {
    return <CatalogContext.Provider value={value}>{children}</CatalogContext.Provider>;
}

export function useCatalog() {
    const value = useContext(CatalogContext);
    if (!value) {
        throw new Error('CatalogProvider is missing');
    }

    return value;
}

export function withQuery(path: string, parameters: Record<string, string | number | undefined>) {
    const query = new URLSearchParams();
    Object.entries(parameters).forEach(([key, value]) => {
        if (value !== undefined && value !== '') {
            query.set(key, String(value));
        }
    });
    const suffix = query.toString();
    return suffix ? `${path}?${suffix}` : path;
}

const copy = {
    en: {
        empty: 'Nothing to show yet.',
        error: 'This section is temporarily unavailable.',
        information: 'More info',
        loading: 'Loading',
        play: 'Play',
        resume: 'Resume',
        retry: 'Try again'
    },
    fr: {
        empty: 'Rien à afficher pour le moment.',
        error: 'Cette section est temporairement indisponible.',
        information: 'Plus d’infos',
        loading: 'Chargement',
        play: 'Lecture',
        resume: 'Reprendre',
        retry: 'Réessayer'
    }
} as const;

export function catalogCopy(locale: CatalogLocale) {
    return copy[locale];
}

export function formatRuntime(ticks?: number, locale: CatalogLocale = 'fr') {
    if (!ticks || ticks <= 0) return '';
    const minutes = Math.round(ticks / 600_000_000);
    if (minutes < 60) return `${minutes} min`;
    const hours = Math.floor(minutes / 60);
    const rest = minutes % 60;
    return locale === 'fr' ? `${hours} h ${String(rest).padStart(2, '0')}` : `${hours}h ${String(rest).padStart(2, '0')}m`;
}

export function itemSubtitle(item: MediaItem, locale: CatalogLocale) {
    const episode = item.Type === 'Episode' && item.ParentIndexNumber !== undefined
        ? `S${item.ParentIndexNumber}:E${item.IndexNumber ?? 0}`
        : '';
    return [episode, item.ProductionYear, formatRuntime(item.RunTimeTicks, locale)]
        .filter(Boolean)
        .join(' · ');
}

export function StatePanel({
    kind,
    onRetry
}: {
    kind: 'empty' | 'error' | 'loading';
    onRetry?: (() => void) | undefined;
}) {
    const { locale } = useCatalog();
    const text = catalogCopy(locale);
    if (kind === 'loading') {
        return (
            <div aria-label={text.loading} className={styles.skeletonGrid} role="status">
                {Array.from({ length: 6 }, (_, index) => <span className={styles.skeleton} key={index} />)}
            </div>
        );
    }

    return (
        <div className={styles.state} role={kind === 'error' ? 'alert' : 'status'}>
            <p>{text[kind]}</p>
            {kind === 'error' && onRetry && <button onClick={onRetry} type="button">{text.retry}</button>}
        </div>
    );
}

export function MediaCard({
    action,
    item,
    layout = 'portrait',
    progress,
    rank
}: {
    action?: ReactNode | undefined;
    item: MediaItem;
    layout?: 'landscape' | 'portrait' | undefined;
    progress?: WatchProgress | null | undefined;
    rank?: number | undefined;
}) {
    const { client, locale } = useCatalog();
    if (!item.Id) return null;

    const image = client.imageUrl(item, layout === 'landscape' ? 'Backdrop' : 'Primary', layout === 'landscape' ? 640 : 360);
    const percent = Math.min(100, Math.max(0, progress?.PercentViewed ?? 0));
    return (
        <article className={`${styles.card} ${styles[layout]}`}>
            <Link className={styles.cardLink} to={`/title/${item.Id}`} viewTransition>
                {rank && <span aria-label={`Top ${rank}`} className={styles.rank}>{String(rank).padStart(2, '0')}</span>}
                <span className={styles.artwork}>
                    {image ? <img alt="" loading="lazy" src={image} /> : <span className={styles.placeholder} />}
                    {percent > 0 && percent < 100 && (
                        <span aria-label={`${Math.round(percent)}%`} className={styles.progress}>
                            <span style={{ width: `${percent}%` }} />
                        </span>
                    )}
                </span>
                <strong>{item.Name}</strong>
                <small>{itemSubtitle(item, locale)}</small>
            </Link>
            {action}
        </article>
    );
}

export function MediaGrid({
    items,
    progressByItem
}: {
    items: MediaItem[];
    progressByItem?: ReadonlyMap<string, WatchProgress>;
}) {
    return (
        <div className={styles.grid}>
            {items.map(item => item.Id
                ? <MediaCard item={item} key={item.Id} progress={progressByItem?.get(item.Id)} />
                : null)}
        </div>
    );
}

export function PageHeading({ children, description }: PropsWithChildren<{ description?: string | undefined }>) {
    return (
        <header className={styles.heading}>
            <h1>{children}</h1>
            {description && <p>{description}</p>}
        </header>
    );
}
