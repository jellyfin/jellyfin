import { useQuery } from '@tanstack/react-query';

import {
    MediaCard,
    MediaGrid,
    type MediaItem,
    PageHeading,
    StatePanel,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './NewAndPopularPage.module.css';

interface RankedResponse {
    Items: Array<{ Item: MediaItem; Rank: number; Score: number }>;
}

interface ItemsResponse {
    Items: MediaItem[];
}

const text = {
    en: { heading: 'New & popular', new: 'Recently added', top: 'Top 10', trending: 'Trending now' },
    fr: { heading: 'Nouveautés', new: 'Ajouts récents', top: 'Top 10', trending: 'Tendances du moment' }
} as const;

export function NewAndPopularPage() {
    const { client, locale, profileId, userId } = useCatalog();
    const scope = { profileId, userId };
    const top = useQuery({
        queryFn: ({ signal }) => client.request<RankedResponse>('CustomNetflix/v1/top10', { signal }),
        queryKey: catalogKeys.newAndPopular(scope, 'top10')
    });
    const trending = useQuery({
        queryFn: ({ signal }) => client.request<RankedResponse>(
            withQuery('CustomNetflix/v1/trending', { limit: 24 }),
            { signal }
        ),
        queryKey: catalogKeys.newAndPopular(scope, 'trending')
    });
    const recent = useQuery({
        queryFn: ({ signal }) => client.request<ItemsResponse>(
            withQuery(`Users/${encodeURIComponent(userId)}/Items`, {
                Fields: 'Overview,Genres,PrimaryImageAspectRatio,ProductionYear,PremiereDate',
                IncludeItemTypes: 'Movie,Series',
                Limit: 36,
                Recursive: 'true',
                SortBy: 'DateCreated',
                SortOrder: 'Descending'
            }),
            { signal }
        ),
        queryKey: catalogKeys.newAndPopular(scope, 'new')
    });
    const pending = top.isPending || trending.isPending || recent.isPending;
    const failed = top.isError || trending.isError || recent.isError;
    const empty = !top.data?.Items.length && !trending.data?.Items.length && !recent.data?.Items.length;

    return (
        <main>
            <PageHeading>{text[locale].heading}</PageHeading>
            {pending && <StatePanel kind="loading" />}
            {failed && (
                <StatePanel
                    kind="error"
                    onRetry={() => void Promise.all([top.refetch(), trending.refetch(), recent.refetch()])}
                />
            )}
            {!pending && !failed && empty && <StatePanel kind="empty" />}
            {top.data?.Items.length ? (
                <section className={styles.section}>
                    <h2>{text[locale].top}</h2>
                    <div className={styles.rail}>
                        {top.data.Items.map(({ Item, Rank }) => Item.Id
                            ? <MediaCard item={Item} key={Item.Id} rank={Rank} />
                            : null)}
                    </div>
                </section>
            ) : null}
            {trending.data?.Items.length ? (
                <section className={styles.section}>
                    <h2>{text[locale].trending}</h2>
                    <MediaGrid items={trending.data.Items.map(item => item.Item)} />
                </section>
            ) : null}
            {recent.data?.Items.length ? (
                <section className={styles.section}>
                    <h2>{text[locale].new}</h2>
                    <MediaGrid items={recent.data.Items} />
                </section>
            ) : null}
        </main>
    );
}
