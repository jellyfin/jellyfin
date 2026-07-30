import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';

import { useSession } from '../auth';
import { useCatalog, withQuery, type MediaItem, type WatchProgress } from '../features/catalog/catalog';
import { catalogKeys } from '../features/catalog/queryKeys';
import { useProfile } from '../features/profiles';
import { WatchPlayer } from '../features/watch';
import { useI18n } from '../i18n';
import { usePlaybackClient } from '../app/runtime';
import { useOnlineStatus } from '../app/useOnlineStatus';
import { StatusView } from '../components/StatusView';
import { LoadingPage } from './SystemPages';

interface WatchDetails {
    Item: MediaItem;
    Progress?: WatchProgress | null;
}

export function WatchPage() {
    const { itemId = '' } = useParams();
    const navigate = useNavigate();
    const queryClient = useQueryClient();
    const { client, profileId, userId } = useCatalog();
    const { activeProfile } = useProfile();
    const { logout } = useSession();
    const { locale } = useI18n();
    const online = useOnlineStatus();
    const playbackClient = usePlaybackClient();
    const scope = { profileId, userId };
    const query = useQuery({
        enabled: Boolean(itemId),
        queryFn: ({ signal }) => client.request<WatchDetails>(
            withQuery(`CustomNetflix/v1/items/${encodeURIComponent(itemId)}/details`, { profileId }),
            { signal }
        ),
        queryKey: catalogKeys.item(scope, itemId)
    });

    useEffect(() => () => {
        void queryClient.invalidateQueries({ queryKey: [ 'catalog', userId, profileId ] });
    }, [ profileId, queryClient, userId ]);

    if (!online) {
        return <StatusView detail={locale === 'fr'
            ? 'La lecture est indisponible hors connexion.'
            : 'Playback is unavailable while offline.'} />;
    }
    if (query.isPending || !activeProfile) return <LoadingPage />;
    if (query.isError || !query.data.Item.Id) {
        return <StatusView action={() => void query.refetch()} />;
    }

    const item = query.data.Item;
    return (
        <WatchPlayer
            client={playbackClient}
            item={{
                Id: item.Id!,
                ...(item.Name === undefined ? {} : { Name: item.Name }),
                ...(item.RunTimeTicks === undefined ? {} : { RunTimeTicks: item.RunTimeTicks }),
                ...(item.Type === undefined ? {} : { Type: item.Type })
            }}
            key={item.Id}
            locale={locale}
            onClose={() => navigate('/home')}
            onPlayNext={(nextId, resumePositionSeconds) => {
                navigate(`/watch/${nextId}`, {
                    replace: true,
                    state: { resumePositionSeconds }
                });
            }}
            onSessionExpired={() => void logout()}
            preferences={activeProfile.PlaybackPreferences}
            profileId={profileId}
            resumePositionSeconds={query.data.Progress?.PositionSeconds ?? 0}
            settings={activeProfile.Settings}
            userId={userId}
        />
    );
}
