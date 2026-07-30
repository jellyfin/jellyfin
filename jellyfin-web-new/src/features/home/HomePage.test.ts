import { describe, expect, it } from 'vitest';

import { selectHero, type HomeRow } from './HomePage';

const row = (Id: string, itemId: string): HomeRow => ({
    Id,
    Items: [{ Item: { Id: itemId, Name: itemId } }],
    Title: Id
});

describe('selectHero', () => {
    it('prefers recommendations, then trending, then new releases', () => {
        expect(selectHero([row('new', 'new'), row('trending', 'trend'), row('recommended-for-you', 'personal')])?.Item.Id)
            .toBe('personal');
        expect(selectHero([row('new', 'new'), row('trending', 'trend')])?.Item.Id)
            .toBe('trend');
        expect(selectHero([row('new', 'new')])?.Item.Id)
            .toBe('new');
    });
});
