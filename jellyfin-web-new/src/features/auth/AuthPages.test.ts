import { describe, expect, it } from 'vitest';

import { validateRegistration } from './AuthPages';

describe('validateRegistration', () => {
    it('applies required name, minimum password and confirmation in order', () => {
        expect(validateRegistration({
            confirmation: 'password',
            password: 'password',
            username: ' '
        }, 8)).toBe('username');
        expect(validateRegistration({
            confirmation: 'short',
            password: 'short',
            username: 'alice'
        }, 8)).toBe('password');
        expect(validateRegistration({
            confirmation: 'different',
            password: 'password',
            username: 'alice'
        }, 8)).toBe('confirmation');
        expect(validateRegistration({
            confirmation: 'password',
            password: 'password',
            username: 'alice'
        }, 8)).toBeNull();
    });
});
