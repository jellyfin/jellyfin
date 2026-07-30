import { describe, expect, it } from 'vitest';

import { deriveApiBaseUrl } from './baseUrl';

describe('deriveApiBaseUrl', () => {
    it.each([
        ['https://media.example/web/', 'https://media.example'],
        ['https://media.example/web/index.html#/home', 'https://media.example'],
        ['https://media.example/emby/web/', 'https://media.example/emby'],
        ['https://media.example/emby/web/index.html?x=1#/login', 'https://media.example/emby'],
        ['https://media.example/emby/', 'https://media.example/emby']
    ])('derives %s', (input, expected) => {
        expect(deriveApiBaseUrl(input)).toBe(expected);
    });
});
