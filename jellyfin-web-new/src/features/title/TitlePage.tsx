import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { useState } from 'react';

import { useOnlineStatus } from '../../app/useOnlineStatus';
import {
    formatRuntime,
    MediaCard,
    type MediaItem,
    StatePanel,
    type WatchProgress,
    useCatalog,
    withQuery
} from '../catalog/catalog';
import { catalogKeys } from '../catalog/queryKeys';
import { FeedbackButtons } from '../feedback/FeedbackButtons';
import { readMetadataSnapshot, writeMetadataSnapshot } from '../../pwa';
import styles from './TitlePage.module.css';

interface DetailsResponse {
    GeneratedAt: string;
    Item: MediaItem;
    ProfileId: string;
    Progress?: WatchProgress | null;
}

interface ItemsResponse {
    Items: MediaItem[];
}

interface ProgressResponse {
    Items: WatchProgress[];
}

interface MyListStatus {
    AddedAt?: string | null;
    IsInMyList: boolean;
    ItemId: string;
    ProfileId: string;
}

const text = {
    en: {
        add: 'Add to my list',
        cast: 'Cast',
        episodes: 'Episodes',
        listUnavailable: 'My list is unavailable.',
        markPlayed: 'Mark as watched',
        markUnplayed: 'Mark as unwatched',
        play: 'Play',
        remove: 'Remove from my list',
        resume: 'Resume',
        season: 'Season'
    },
    fr: {
        add: 'Ajouter à ma liste',
        cast: 'Distribution',
        episodes: 'Épisodes',
        listUnavailable: 'Ma liste est indisponible.',
        markPlayed: 'Marquer comme vu',
        markUnplayed: 'Marquer comme non vu',
        play: 'Lecture',
        remove: 'Retirer de ma liste',
        resume: 'Reprendre',
        season: 'Saison'
    }
} as const;

export function TitlePage({ itemId: explicitItemId }: { itemId?: string }) {
    const online = useOnlineStatus();
    const params = useParams();
    const itemId = explicitItemId ?? params.itemId ?? '';
    const { client, locale, profileId, userId } = useCatalog();
    const labels = text[locale];
    const scope = { profileId, userId };
    const queryClient = useQueryClient();
    const detailsKey = catalogKeys.item(scope, itemId);
    const details = useQuery({
        enabled: Boolean(itemId),
        queryFn: async ({ signal }) => {
            try {
                const value = await client.request<DetailsResponse>(
                    withQuery(`CustomNetflix/v1/items/${encodeURIComponent(itemId)}/details`, { profileId }),
                    { signal }
                );
                void writeMetadataSnapshot(userId, profileId, 'title', value, itemId)
                    .catch(() => undefined);
                return value;
            } catch (error) {
                if (!navigator.onLine) {
                    const snapshot = await readMetadataSnapshot<DetailsResponse>(
                        userId,
                        profileId,
                        'title',
                        itemId
                    ).catch(() => null);
                    if (snapshot) return snapshot;
                }
                throw error;
            }
        },
        queryKey: detailsKey
    });
    const listKey = catalogKeys.myListStatus(scope, itemId);
    const listStatus = useQuery({
        enabled: Boolean(itemId),
        queryFn: ({ signal }) => client.request<MyListStatus>(
            `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/my-list/${encodeURIComponent(itemId)}`,
            { signal }
        ),
        queryKey: listKey
    });
    const listMutation = useMutation({
        mutationFn: (add: boolean) => client.request<MyListStatus>(
            `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/my-list/${encodeURIComponent(itemId)}`,
            { method: add ? 'PUT' : 'DELETE' }
        ),
        onError: (_error, _add, previous) => queryClient.setQueryData(listKey, previous),
        onMutate: async add => {
            await queryClient.cancelQueries({ queryKey: listKey });
            const previous = queryClient.getQueryData<MyListStatus>(listKey);
            queryClient.setQueryData<MyListStatus>(listKey, current => ({
                AddedAt: current?.AddedAt ?? null,
                IsInMyList: add,
                ItemId: itemId,
                ProfileId: profileId
            }));
            return previous;
        },
        onSettled: () => {
            void queryClient.invalidateQueries({ queryKey: listKey });
            void queryClient.invalidateQueries({ queryKey: catalogKeys.myList(scope) });
            void queryClient.invalidateQueries({ queryKey: catalogKeys.home(scope) });
        }
    });
    const playedMutation = useMutation({
        mutationFn: (played: boolean) => client.request<WatchProgress>(
            `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/items/${encodeURIComponent(itemId)}/played`,
            { body: { Played: played }, method: 'POST' }
        ),
        onSuccess: progress => queryClient.setQueryData<DetailsResponse>(detailsKey, current => current
            ? { ...current, Progress: progress }
            : current)
    });

    const item = details.data?.Item;
    const isSeries = item?.Type === 'Series';
    const seasons = useQuery({
        enabled: isSeries,
        queryFn: ({ signal }) => client.request<ItemsResponse>(
            withQuery(`Shows/${encodeURIComponent(itemId)}/Seasons`, {
                Fields: 'Overview,PrimaryImageAspectRatio,ProductionYear',
                userId
            }),
            { signal }
        ),
        queryKey: catalogKeys.series(scope, itemId)
    });
    const [selectedSeason, setSelectedSeason] = useState('');
    const seasonId = selectedSeason || seasons.data?.Items.find(season => season.Id)?.Id || '';
    const episodes = useQuery({
        enabled: isSeries && Boolean(seasonId),
        queryFn: ({ signal }) => client.request<ItemsResponse>(
            withQuery(`Shows/${encodeURIComponent(itemId)}/Episodes`, {
                Fields: 'Overview,PrimaryImageAspectRatio,ProductionYear,RunTimeTicks',
                seasonId,
                userId
            }),
            { signal }
        ),
        queryKey: catalogKeys.series(scope, itemId, seasonId)
    });
    const episodeIds = (episodes.data?.Items ?? []).flatMap(episode => episode.Id ? [episode.Id] : []);
    const episodeProgress = useQuery({
        enabled: episodeIds.length > 0,
        queryFn: ({ signal }) => client.request<ProgressResponse>(
            `CustomNetflix/v1/profiles/${encodeURIComponent(profileId)}/progress/batch`,
            { body: { ItemIds: episodeIds }, method: 'POST', signal }
        ),
        queryKey: catalogKeys.progress(scope, episodeIds)
    });

    if (!itemId) return <StatePanel kind="error" />;
    if (details.isPending) return <StatePanel kind="loading" />;
    if (details.isError) return <StatePanel kind="error" onRetry={() => void details.refetch()} />;
    if (!item) return <StatePanel kind="empty" />;

    const backdrop = client.imageUrl(item, 'Backdrop', 1920);
    const metadata = [
        item.ProductionYear,
        formatRuntime(item.RunTimeTicks, locale),
        item.OfficialRating,
        item.CommunityRating ? `${item.CommunityRating.toFixed(1)}/10` : ''
    ].filter(Boolean).join(' · ');
    const cast = (item.People ?? []).filter(person => person.Type === 'Actor').slice(0, 8);
    const progressByItem = new Map((episodeProgress.data?.Items ?? []).map(progress => [progress.ItemId, progress]));
    const isInList = listStatus.data?.IsInMyList ?? false;
    const completed = details.data.Progress?.Completed ?? false;
    const playCopy = details.data.Progress?.PositionSeconds ? labels.resume : labels.play;

    return (
        <main className={styles.page}>
            <section
                className={styles.backdrop}
                style={backdrop ? { backgroundImage: `url("${backdrop}")` } : undefined}
            />
            <article className={styles.sheet}>
                <p className={styles.type}>{item.Type}</p>
                <h1>{item.Name}</h1>
                <p className={styles.metadata}>{metadata}</p>
                <div className={styles.actions}>
                    <Link
                        aria-disabled={!online}
                        className={styles.play}
                        onClick={event => {
                            if (!online) event.preventDefault();
                        }}
                        to={`/watch/${item.Id}`}
                    >
                        {playCopy}
                    </Link>
                    <button
                        aria-pressed={isInList}
                        disabled={!online || listStatus.isPending || listStatus.isError || listMutation.isPending}
                        onClick={() => listMutation.mutate(!isInList)}
                        type="button"
                    >
                        {isInList ? '✓' : '+'} {isInList ? labels.remove : labels.add}
                    </button>
                    <button
                        disabled={!online || playedMutation.isPending}
                        onClick={() => playedMutation.mutate(!completed)}
                        type="button"
                    >
                        {completed ? labels.markUnplayed : labels.markPlayed}
                    </button>
                </div>
                {listStatus.isError && <small className={styles.inlineError} role="alert">{labels.listUnavailable}</small>}
                <FeedbackButtons itemId={itemId} />
                {item.Overview && <p className={styles.overview}>{item.Overview}</p>}
                {item.Genres?.length ? <p className={styles.genres}>{item.Genres.join(' · ')}</p> : null}
                {cast.length ? (
                    <section className={styles.cast}>
                        <h2>{labels.cast}</h2>
                        <p>{cast.map(person => person.Name).filter(Boolean).join(', ')}</p>
                    </section>
                ) : null}
                {isSeries && (
                    <section className={styles.episodes}>
                        <div className={styles.episodeHeading}>
                            <h2>{labels.episodes}</h2>
                            {seasons.data?.Items.length ? (
                                <label>
                                    <span>{labels.season}</span>
                                    <select onChange={event => setSelectedSeason(event.target.value)} value={seasonId}>
                                        {seasons.data.Items.map((season, index) => season.Id ? (
                                            <option key={season.Id} value={season.Id}>
                                                {season.Name ?? `${labels.season} ${index + 1}`}
                                            </option>
                                        ) : null)}
                                    </select>
                                </label>
                            ) : null}
                        </div>
                        {(seasons.isPending || episodes.isPending) && <StatePanel kind="loading" />}
                        {(seasons.isError || episodes.isError) && (
                            <StatePanel
                                kind="error"
                                onRetry={() => void (seasons.isError ? seasons.refetch() : episodes.refetch())}
                            />
                        )}
                        {episodes.isSuccess && !episodes.data.Items.length && <StatePanel kind="empty" />}
                        {episodes.data?.Items.length ? (
                            <div className={styles.episodeGrid}>
                                {episodes.data.Items.map(episode => episode.Id ? (
                                    <MediaCard
                                        item={episode}
                                        key={episode.Id}
                                        layout="landscape"
                                        progress={progressByItem.get(episode.Id)}
                                    />
                                ) : null)}
                            </div>
                        ) : null}
                    </section>
                )}
            </article>
        </main>
    );
}
