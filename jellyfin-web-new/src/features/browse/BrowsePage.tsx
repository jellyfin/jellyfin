import { type InfiniteData, useInfiniteQuery, useQuery } from '@tanstack/react-query';
import { useState } from 'react';

import {
    MediaGrid,
    type MediaItem,
    PageHeading,
    StatePanel,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './BrowsePage.module.css';

type BrowseKind = 'Movie' | 'Series';
type Sort = 'alphabetical' | 'popular' | 'rating' | 'recent';

interface ItemsResponse {
    Items: MediaItem[];
    StartIndex?: number;
    TotalRecordCount?: number;
}

interface GenreResponse {
    Items: Array<{ Name?: string }>;
}

const pageSize = 36;
const fields = 'Overview,Genres,PrimaryImageAspectRatio,ProductionYear,CommunityRating,DateCreated,PremiereDate';
const sortApi: Record<Sort, string> = {
    alphabetical: 'SortName',
    popular: 'PlayCount',
    rating: 'CommunityRating',
    recent: 'DateCreated'
};

const text = {
    en: {
        alphabetical: 'A–Z',
        allGenres: 'All genres',
        genre: 'Genre',
        loadMore: 'Load more',
        movies: 'Movies',
        popular: 'Popular',
        rating: 'Rating',
        recent: 'Recently added',
        series: 'Series',
        sort: 'Sort',
        year: 'Year'
    },
    fr: {
        alphabetical: 'A–Z',
        allGenres: 'Tous les genres',
        genre: 'Genre',
        loadMore: 'Afficher plus',
        movies: 'Films',
        popular: 'Populaires',
        rating: 'Note',
        recent: 'Ajouts récents',
        series: 'Séries',
        sort: 'Trier',
        year: 'Année'
    }
} as const;

export function BrowsePage({ type }: { type: BrowseKind }) {
    const { client, locale, profileId, userId } = useCatalog();
    const labels = text[locale];
    const scope = { profileId, userId };
    const [genre, setGenre] = useState('');
    const [sort, setSort] = useState<Sort>('popular');
    const [year, setYear] = useState('');
    const filters = { genre, sort, year };
    const browseKey = catalogKeys.browse(scope, type, filters);

    const genres = useQuery({
        queryFn: ({ signal }) => client.request<GenreResponse>(withQuery('Genres', {
            IncludeItemTypes: type,
            Recursive: 'true',
            userId
        }), { signal }),
        queryKey: catalogKeys.genres(scope, type),
        staleTime: 60 * 60_000
    });

    const query = useInfiniteQuery<
        ItemsResponse,
        Error,
        InfiniteData<ItemsResponse, number>,
        typeof browseKey,
        number
    >({
        getNextPageParam: (lastPage, pages) => {
            const loaded = pages.reduce((sum, page) => sum + page.Items.length, 0);
            return loaded < (lastPage.TotalRecordCount ?? loaded) ? loaded : undefined;
        },
        initialPageParam: 0,
        queryFn: ({ pageParam, signal }) => client.request<ItemsResponse>(
            withQuery(`Users/${encodeURIComponent(userId)}/Items`, {
                EnableImageTypes: 'Primary,Backdrop',
                EnableTotalRecordCount: 'true',
                Fields: fields,
                Genres: genre || undefined,
                IncludeItemTypes: type,
                Limit: pageSize,
                Recursive: 'true',
                SortBy: sortApi[sort],
                SortOrder: sort === 'alphabetical' ? 'Ascending' : 'Descending',
                StartIndex: pageParam,
                Years: year || undefined
            }),
            { signal }
        ),
        queryKey: browseKey,
        staleTime: 60_000
    });

    const items = query.data?.pages.flatMap(page => page.Items) ?? [];
    return (
        <main>
            <PageHeading>{type === 'Movie' ? labels.movies : labels.series}</PageHeading>
            <div className={styles.filters}>
                <label>
                    <span>{labels.genre}</span>
                    <select onChange={event => setGenre(event.target.value)} value={genre}>
                        <option value="">{labels.allGenres}</option>
                        {(genres.data?.Items ?? []).map(item => item.Name
                            ? <option key={item.Name} value={item.Name}>{item.Name}</option>
                            : null)}
                    </select>
                </label>
                <label>
                    <span>{labels.year}</span>
                    <input
                        inputMode="numeric"
                        max={new Date().getFullYear() + 1}
                        min="1888"
                        onChange={event => setYear(event.target.value)}
                        placeholder="2026"
                        type="number"
                        value={year}
                    />
                </label>
                <label>
                    <span>{labels.sort}</span>
                    <select onChange={event => setSort(event.target.value as Sort)} value={sort}>
                        <option value="popular">{labels.popular}</option>
                        <option value="recent">{labels.recent}</option>
                        <option value="rating">{labels.rating}</option>
                        <option value="alphabetical">{labels.alphabetical}</option>
                    </select>
                </label>
            </div>

            {query.isPending && <StatePanel kind="loading" />}
            {query.isError && <StatePanel kind="error" onRetry={() => void query.refetch()} />}
            {query.isSuccess && !items.length && <StatePanel kind="empty" />}
            {items.length > 0 && <MediaGrid items={items} />}
            {query.hasNextPage && (
                <button
                    className={styles.more}
                    disabled={query.isFetchingNextPage}
                    onClick={() => void query.fetchNextPage()}
                    type="button"
                >
                    {labels.loadMore}
                </button>
            )}
        </main>
    );
}
