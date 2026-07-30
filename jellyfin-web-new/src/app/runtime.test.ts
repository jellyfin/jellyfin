import { describe, expect, it } from 'vitest';

import { buildMediaUrl } from './runtime';

describe('buildMediaUrl', () => {
    it('adds the Jellyfin token only to same-origin media URLs', () => {
        expect(buildMediaUrl(
            'https://media.example/emby',
            'Videos/item/stream',
            { quality: 90 },
            'secret'
        )).toBe('https://media.example/emby/Videos/item/stream?quality=90&api_key=secret');

        expect(buildMediaUrl(
            'https://media.example/emby',
            'https://cdn.example/video.m3u8?signature=signed',
            { ApiKey: 'secret', 'X-Emby-Token': 'other', quality: 90 },
            'secret'
        )).toBe('https://cdn.example/video.m3u8?signature=signed&quality=90');
    });
});
