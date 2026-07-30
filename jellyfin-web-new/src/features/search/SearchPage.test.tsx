import { act, renderHook } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';

import { useDebouncedValue } from './SearchPage';

describe('useDebouncedValue', () => {
    it('publishes only the last value after 250 ms', () => {
        vi.useFakeTimers();
        const { rerender, result } = renderHook(
            ({ value }) => useDebouncedValue(value, 250),
            { initialProps: { value: 'a' } }
        );

        rerender({ value: 'alien' });
        act(() => vi.advanceTimersByTime(249));
        expect(result.current).toBe('a');
        act(() => vi.advanceTimersByTime(1));
        expect(result.current).toBe('alien');
        vi.useRealTimers();
    });
});
