import { useQuery } from '@tanstack/react-query';
import { useEffect, useState } from 'react';

import {
    MediaGrid,
    type MediaItem,
    PageHeading,
    StatePanel,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './SearchPage.module.css';

interface SearchResponse {
    Items: MediaItem[];
}

export function useDebouncedValue<T>(value: T, delay = 250) {
    const [debounced, setDebounced] = useState(value);
    useEffect(() => {
        const timer = window.setTimeout(() => setDebounced(value), delay);
        return () => window.clearTimeout(timer);
    }, [delay, value]);
    return debounced;
}

const text = {
    en: {
        heading: 'Search',
        hint: 'Enter at least two characters.',
        label: 'Search movies and series',
        placeholder: 'Title, actor, director…',
        results: 'Results'
    },
    fr: {
        heading: 'Recherche',
        hint: 'Saisissez au moins deux caractères.',
        label: 'Rechercher des films et séries',
        placeholder: 'Titre, acteur, réalisateur…',
        results: 'Résultats'
    }
} as const;

export function SearchPage() {
    const { client, locale, profileId, userId } = useCatalog();
    const labels = text[locale];
    const [term, setTerm] = useState('');
    const debounced = useDebouncedValue(term.trim(), 250);
    const enabled = debounced.length >= 2;
    const query = useQuery({
        enabled,
        queryFn: ({ signal }) => client.request<SearchResponse>(
            withQuery(`Users/${encodeURIComponent(userId)}/Items`, {
                EnableImageTypes: 'Primary,Backdrop',
                Fields: 'Overview,Genres,PrimaryImageAspectRatio,ProductionYear,CommunityRating,PremiereDate',
                IncludeItemTypes: 'Movie,Series',
                Limit: 60,
                Recursive: 'true',
                SearchTerm: debounced,
                SortBy: 'SortName'
            }),
            { signal }
        ),
        queryKey: catalogKeys.search({ profileId, userId }, debounced)
    });

    return (
        <main>
            <PageHeading>{labels.heading}</PageHeading>
            <div className={styles.search}>
                <label htmlFor="catalog-search">{labels.label}</label>
                <input
                    autoComplete="off"
                    id="catalog-search"
                    onChange={event => setTerm(event.target.value)}
                    placeholder={labels.placeholder}
                    type="search"
                    value={term}
                />
            </div>
            {!enabled && <p className={styles.hint}>{labels.hint}</p>}
            {enabled && query.isPending && <StatePanel kind="loading" />}
            {enabled && query.isError && <StatePanel kind="error" onRetry={() => void query.refetch()} />}
            {query.isSuccess && !query.data.Items.length && <StatePanel kind="empty" />}
            {query.data?.Items.length ? (
                <section aria-label={labels.results}>
                    <MediaGrid items={query.data.Items} />
                </section>
            ) : null}
        </main>
    );
}
