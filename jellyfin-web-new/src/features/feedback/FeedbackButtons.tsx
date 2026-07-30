import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';

import { useOnlineStatus } from '../../app/useOnlineStatus';
import { useCatalog } from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import styles from './FeedbackButtons.module.css';

export type Feedback = 'dislike' | 'like';

interface FeedbackResponse {
    Feedback?: Feedback | 'not-interested' | null;
    ItemId: string;
    ProfileId: string;
    UpdatedAt?: string | null;
}

const text = {
    en: { dislike: 'Not for me', error: 'Could not save your opinion.', like: 'I like this' },
    fr: { dislike: 'Pas pour moi', error: 'Impossible d’enregistrer votre avis.', like: 'J’aime' }
} as const;

export function FeedbackButtons({ itemId }: { itemId: string }) {
    const online = useOnlineStatus();
    const { client, locale, profileId, userId } = useCatalog();
    const queryClient = useQueryClient();
    const key = catalogKeys.feedback({ profileId, userId }, itemId);
    const path = `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/items/${encodeURIComponent(itemId)}/feedback`;
    const query = useQuery({
        queryFn: ({ signal }) => client.request<FeedbackResponse>(path, { signal }),
        queryKey: key
    });
    const mutation = useMutation({
        mutationFn: async (feedback: Feedback | null) => {
            if (feedback === null) {
                await client.request(path, { method: 'DELETE' });
                return { Feedback: null, ItemId: itemId, ProfileId: profileId } satisfies FeedbackResponse;
            }

            return client.request<FeedbackResponse>(path, {
                body: { Feedback: feedback },
                method: 'PUT'
            });
        },
        onError: (_error, _next, previous) => queryClient.setQueryData(key, previous),
        onMutate: async next => {
            await queryClient.cancelQueries({ queryKey: key });
            const previous = queryClient.getQueryData<FeedbackResponse>(key);
            queryClient.setQueryData<FeedbackResponse>(key, {
                Feedback: next,
                ItemId: itemId,
                ProfileId: profileId
            });
            return previous;
        },
        onSettled: () => void queryClient.invalidateQueries({ queryKey: key })
    });

    const selected = query.data?.Feedback;
    const select = (next: Feedback) => mutation.mutate(selected === next ? null : next);
    return (
        <div className={styles.feedback}>
            <button
                aria-pressed={selected === 'like'}
                disabled={!online || mutation.isPending}
                onClick={() => select('like')}
                type="button"
            >
                <span aria-hidden="true">↑</span> {text[locale].like}
            </button>
            <button
                aria-pressed={selected === 'dislike'}
                disabled={!online || mutation.isPending}
                onClick={() => select('dislike')}
                type="button"
            >
                <span aria-hidden="true">↓</span> {text[locale].dislike}
            </button>
            {(query.isError || mutation.isError) && <small role="alert">{text[locale].error}</small>}
        </div>
    );
}
