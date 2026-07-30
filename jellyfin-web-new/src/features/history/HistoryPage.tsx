import { useQuery } from '@tanstack/react-query';

import {
    MediaCard,
    type MediaItem,
    PageHeading,
    StatePanel,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './HistoryPage.module.css';

interface HistoryItem {
    History: {
        CompletedAt?: string | null;
        FirstPlayedAt: string;
        LastPlayedAt: string;
        PlayCount: number;
        ProfileId: string;
    };
    Item: MediaItem;
}

interface HistoryResponse {
    Items: HistoryItem[];
}

const text = {
    en: { heading: 'Viewing history', played: 'Last watched' },
    fr: { heading: 'Historique', played: 'Dernière lecture' }
} as const;

export function HistoryPage() {
    const { client, locale, profileId, userId } = useCatalog();
    const query = useQuery({
        queryFn: ({ signal }) => client.request<HistoryResponse>(
            withQuery(`CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/history`, { limit: 100 }),
            { signal }
        ),
        queryKey: catalogKeys.history({ profileId, userId })
    });

    return (
        <main>
            <PageHeading>{text[locale].heading}</PageHeading>
            {query.isPending && <StatePanel kind="loading" />}
            {query.isError && <StatePanel kind="error" onRetry={() => void query.refetch()} />}
            {query.isSuccess && !query.data.Items.length && <StatePanel kind="empty" />}
            {query.data?.Items.length ? (
                <div className={styles.grid}>
                    {query.data.Items.map(({ History, Item }) => Item.Id ? (
                        <div key={`${Item.Id}-${History.LastPlayedAt}`}>
                            <MediaCard item={Item} />
                            <p>
                                {text[locale].played}{' '}
                                <time dateTime={History.LastPlayedAt}>
                                    {new Intl.DateTimeFormat(locale, { dateStyle: 'medium' }).format(new Date(History.LastPlayedAt))}
                                </time>
                            </p>
                        </div>
                    ) : null)}
                </div>
            ) : null}
        </main>
    );
}
