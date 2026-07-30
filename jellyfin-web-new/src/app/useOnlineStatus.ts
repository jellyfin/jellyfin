import { useSyncExternalStore } from 'react';

function subscribe(onChange: () => void) {
    window.addEventListener('online', onChange);
    window.addEventListener('offline', onChange);
    return () => {
        window.removeEventListener('online', onChange);
        window.removeEventListener('offline', onChange);
    };
}

export function useOnlineStatus() {
    return useSyncExternalStore(subscribe, () => navigator.onLine, () => true);
}
