import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';

import {
    catalogCopy,
    itemSubtitle,
    MediaCard,
    type MediaItem,
    StatePanel,
    type WatchProgress,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import { readMetadataSnapshot, writeMetadataSnapshot } from '../../pwa';
import styles from './HomePage.module.css';

export interface HomeItem {
    Item: MediaItem;
    Progress?: WatchProgress | null;
    RecommendationReason?: string;
}

export interface HomeRow {
    Id: string;
    Items: HomeItem[];
    Title: string;
    TitleKey?: string;
}

export interface HomeResponse {
    GeneratedAt: string;
    ProfileId: string;
    Rows: HomeRow[];
}

const rowNames = {
    en: {
        'continue-watching': 'Continue watching',
        'my-list': 'My list',
        'new': 'New releases',
        'popular-movies': 'Popular movies',
        'popular-series': 'Popular series',
        'recommended-for-you': 'For you',
        discover: 'Discover',
        top10: 'Top 10',
        trending: 'Trending'
    },
    fr: {
        'continue-watching': 'Reprendre',
        'my-list': 'Ma liste',
        'new': 'Nouveautés',
        'popular-movies': 'Films populaires',
        'popular-series': 'Séries populaires',
        'recommended-for-you': 'Pour ce profil',
        discover: 'À découvrir',
        top10: 'Top 10',
        trending: 'Tendances'
    }
} as const;

function rowIdentity(row: HomeRow) {
    return `${row.Id} ${row.Title} ${row.TitleKey ?? ''}`.toLowerCase();
}

export function selectHero(rows: HomeRow[]) {
    const priorities = [
        ['recommend', 'for-you', 'discover'],
        ['trending'],
        ['new']
    ];

    for (const aliases of priorities) {
        const row = rows.find(candidate => aliases.some(alias => rowIdentity(candidate).includes(alias)));
        const item = row?.Items.find(candidate => Boolean(candidate.Item.Id));
        if (item) return item;
    }

    return undefined;
}

function rowTitle(row: HomeRow, locale: 'en' | 'fr') {
    return rowNames[locale][row.Id as keyof typeof rowNames.en] ?? row.Title;
}

function isLandscape(row: HomeRow) {
    return row.Id === 'continue-watching'
        || row.Id === 'trending'
        || row.Id === 'recommended-for-you'
        || row.Id === 'discover';
}

function Hero({ homeItem }: { homeItem: HomeItem }) {
    const { client, locale } = useCatalog();
    const { Item: item, Progress: progress } = homeItem;
    const copy = catalogCopy(locale);
    const backdrop = client.imageUrl(item, 'Backdrop', 1920);
    if (!item.Id) return null;

    return (
        <section
            className={styles.hero}
            style={backdrop ? { backgroundImage: `url("${backdrop}")` } : undefined}
        >
            <div className={styles.heroContent}>
                <p className={styles.eyebrow}>{locale === 'fr' ? 'À voir ce soir' : 'Tonight’s feature'}</p>
                <h1>{item.Name}</h1>
                <p className={styles.metadata}>{itemSubtitle(item, locale)}</p>
                {item.Overview && <p className={styles.overview}>{item.Overview}</p>}
                <div className={styles.heroActions}>
                    <Link className={styles.primary} to={`/watch/${item.Id}`}>
                        {progress?.PositionSeconds ? copy.resume : copy.play}
                    </Link>
                    <Link className={styles.secondary} to={`/title/${item.Id}`} viewTransition>{copy.information}</Link>
                </div>
            </div>
        </section>
    );
}

export function HomePage() {
    const { client, locale, profileId, userId } = useCatalog();
    const scope = { profileId, userId };
    const query = useQuery({
        queryFn: async ({ signal }) => {
            try {
                const home = await client.request<HomeResponse>(
                    withQuery('CustomNetflix/v1/home', { itemLimit: 24, profileId }),
                    { signal }
                );
                void writeMetadataSnapshot(userId, profileId, 'home', home).catch(() => undefined);
                return home;
            } catch (error) {
                if (!navigator.onLine) {
                    const snapshot = await readMetadataSnapshot<HomeResponse>(userId, profileId, 'home')
                        .catch(() => null);
                    if (snapshot) return snapshot;
                }
                throw error;
            }
        },
        queryKey: catalogKeys.home(scope),
        staleTime: 60_000
    });

    if (query.isPending) return <StatePanel kind="loading" />;
    if (query.isError) return <StatePanel kind="error" onRetry={() => void query.refetch()} />;

    const rows = query.data.Rows
        .map(row => ({ ...row, Items: row.Items.filter(({ Item }) => Boolean(Item.Id)) }))
        .filter(row => row.Items.length > 0);
    if (!rows.length) return <StatePanel kind="empty" />;

    const hero = selectHero(rows);
    return (
        <main>
            {hero && <Hero homeItem={hero} />}
            <div className={styles.rows}>
                {rows.map(row => (
                    <section className={styles.rail} key={row.Id}>
                        <h2>{rowTitle(row, locale)}</h2>
                        <div className={styles.scroller}>
                            {row.Items.map(({ Item, Progress }, index) => (
                                <MediaCard
                                    item={Item}
                                    key={Item.Id}
                                    layout={isLandscape(row) ? 'landscape' : 'portrait'}
                                    progress={Progress}
                                    rank={row.Id === 'top10' ? index + 1 : undefined}
                                />
                            ))}
                        </div>
                    </section>
                ))}
            </div>
        </main>
    );
}
