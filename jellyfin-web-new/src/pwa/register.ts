import { playerStore } from '../player/store';

export type InstallUpdate = () => Promise<void>;

/**
 * Registration is explicit so the app can show its own update prompt. A prompt
 * discovered during playback is held until the player is idle.
 */
export async function registerPwa(
    onUpdateAvailable: (install: InstallUpdate) => void
) {
    if (!('serviceWorker' in navigator)) return () => undefined;
    const { registerSW } = await import('virtual:pwa-register');
    let unsubscribe: (() => void) | undefined;
    const update = registerSW({
        immediate: true,
        onNeedRefresh() {
            const offer = () => onUpdateAvailable(() => update(true));
            if (!playerStore.isActive()) {
                offer();
                return;
            }
            unsubscribe?.();
            unsubscribe = playerStore.subscribe(() => {
                if (!playerStore.isActive()) {
                    unsubscribe?.();
                    unsubscribe = undefined;
                    offer();
                }
            });
        }
    });
    return () => {
        unsubscribe?.();
    };
}
