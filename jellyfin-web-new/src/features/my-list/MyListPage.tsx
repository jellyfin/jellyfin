import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useOnlineStatus } from '../../app/useOnlineStatus';
import {
    MediaCard,
    type MediaItem,
    PageHeading,
    StatePanel,
    type WatchProgress,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './MyListPage.module.css';

export interface MyListItem {
    AddedAt: string;
    Item: MediaItem;
    Progress?: WatchProgress | null;
}

export interface MyListResponse {
    Items: MyListItem[];
    ProfileId: string;
}

const text = {
    en: { heading: 'My list', remove: 'Remove from my list' },
    fr: { heading: 'Ma liste', remove: 'Retirer de ma liste' }
} as const;

export function MyListPage() {
    const online = useOnlineStatus();
    const { client, locale, profileId, userId } = useCatalog();
    const scope = { profileId, userId };
    const key = catalogKeys.myList(scope);
    const queryClient = useQueryClient();
    const query = useQuery({
        queryFn: ({ signal }) => client.request<MyListResponse>(
            withQuery(`CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/my-list`, { limit: 100 }),
            { signal }
        ),
        queryKey: key
    });
    const remove = useMutation({
        mutationFn: (itemId: string) => client.request(
            `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/my-list/${encodeURIComponent(itemId)}`,
            { method: 'DELETE' }
        ),
        onError: (_error, _itemId, previous) => {
            if (previous) queryClient.setQueryData(key, previous);
        },
        onMutate: async itemId => {
            await queryClient.cancelQueries({ queryKey: key });
            const previous = queryClient.getQueryData<MyListResponse>(key);
            queryClient.setQueryData<MyListResponse>(key, current => current
                ? { ...current, Items: current.Items.filter(({ Item }) => Item.Id !== itemId) }
                : current);
            return previous;
        },
        onSettled: () => void queryClient.invalidateQueries({ queryKey: key })
    });

    return (
        <main>
            <PageHeading>{text[locale].heading}</PageHeading>
            {query.isPending && <StatePanel kind="loading" />}
            {query.isError && <StatePanel kind="error" onRetry={() => void query.refetch()} />}
            {query.isSuccess && !query.data.Items.length && <StatePanel kind="empty" />}
            {query.data?.Items.length ? (
                <div className={styles.grid}>
                    {query.data.Items.map(({ Item, Progress }) => Item.Id ? (
                        <MediaCard
                            action={(
                                <button
                                    aria-label={`${text[locale].remove}: ${Item.Name ?? ''}`}
                                    disabled={!online || (remove.isPending && remove.variables === Item.Id)}
                                    onClick={() => remove.mutate(Item.Id!)}
                                    type="button"
                                >
                                    ×
                                </button>
                            )}
                            item={Item}
                            key={Item.Id}
                            progress={Progress}
                        />
                    ) : null)}
                </div>
            ) : null}
        </main>
    );
}
