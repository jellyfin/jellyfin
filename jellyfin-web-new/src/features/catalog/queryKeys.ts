export interface CatalogScope {
    profileId: string;
    userId: string;
}

const root = ({ profileId, userId }: CatalogScope) => ['catalog', userId, profileId] as const;

export const catalogKeys = {
    browse: (scope: CatalogScope, mediaType: string, filters: object) =>
        [...root(scope), 'browse', mediaType, filters] as const,
    feedback: (scope: CatalogScope, itemId: string) =>
        [...root(scope), 'feedback', itemId] as const,
    genres: (scope: CatalogScope, mediaType: string) =>
        [...root(scope), 'genres', mediaType] as const,
    history: (scope: CatalogScope) => [...root(scope), 'history'] as const,
    home: (scope: CatalogScope) => [...root(scope), 'home'] as const,
    item: (scope: CatalogScope, itemId: string) =>
        [...root(scope), 'item', itemId] as const,
    myList: (scope: CatalogScope) => [...root(scope), 'my-list'] as const,
    myListStatus: (scope: CatalogScope, itemId: string) =>
        [...root(scope), 'my-list-status', itemId] as const,
    newAndPopular: (scope: CatalogScope, section: string) =>
        [...root(scope), 'new-and-popular', section] as const,
    progress: (scope: CatalogScope, itemIds: string[]) =>
        [...root(scope), 'progress', itemIds] as const,
    search: (scope: CatalogScope, term: string) =>
        [...root(scope), 'search', term] as const,
    series: (scope: CatalogScope, seriesId: string, seasonId?: string) =>
        [...root(scope), 'series', seriesId, seasonId ?? 'seasons'] as const
};
