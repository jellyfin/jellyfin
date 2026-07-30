export type MediaKind = 'Movie' | 'Series' | 'Season' | 'Episode';

export interface MediaSummary {
    id: string;
    name: string;
    type: MediaKind;
    imageUrl?: string;
    backdropUrl?: string;
    overview?: string;
    productionYear?: number;
    officialRating?: string;
    communityRating?: number;
    runTimeTicks?: number;
    progressPercent?: number;
    seriesName?: string;
    seasonName?: string;
    indexNumber?: number;
}

export interface MediaRail {
    id: string;
    title: string;
    items: MediaSummary[];
    numbered?: boolean;
}
