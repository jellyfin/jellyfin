import { describe, expect, it } from 'vitest';

import { catalogKeys } from './queryKeys';

describe('catalog query keys', () => {
    it('isolates every cache by user and active profile', () => {
        const first = catalogKeys.home({ profileId: 'profile-a', userId: 'user-a' });
        const nextProfile = catalogKeys.home({ profileId: 'profile-b', userId: 'user-a' });
        const nextUser = catalogKeys.home({ profileId: 'profile-a', userId: 'user-b' });

        expect(first).not.toEqual(nextProfile);
        expect(first).not.toEqual(nextUser);
        expect(catalogKeys.search({ profileId: 'profile-a', userId: 'user-a' }, 'dune'))
            .toEqual(['catalog', 'user-a', 'profile-a', 'search', 'dune']);
    });
});
