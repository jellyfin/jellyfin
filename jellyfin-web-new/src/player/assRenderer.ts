import SubtitlesOctopus from '@jellyfin/libass-wasm';
import fallbackFontUrl from '@jellyfin/libass-wasm/dist/js/default.woff2?url';
import workerSource from '@jellyfin/libass-wasm/dist/js/subtitles-octopus-worker.js?raw';
import wasmUrl from '@jellyfin/libass-wasm/dist/js/subtitles-octopus-worker.wasm?url';

export function createAssRenderer(
    video: HTMLVideoElement,
    subtitleUrl: string,
    onError: () => void
) {
    const patchedWorker = workerSource.replaceAll(
        '"subtitles-octopus-worker.wasm"',
        JSON.stringify(wasmUrl)
    );
    const workerUrl = URL.createObjectURL(new Blob([ patchedWorker ], {
        type: 'text/javascript'
    }));
    const renderer = new SubtitlesOctopus({
        video,
        subUrl: subtitleUrl,
        workerUrl,
        fallbackFont: fallbackFontUrl,
        renderMode: 'wasm-blend',
        libassMemoryLimit: 40,
        libassGlyphLimit: 40,
        prescaleFactor: 0.8,
        prescaleHeightLimit: 1080,
        maxRenderHeight: 2160,
        renderAhead: 90,
        onError
    });

    return {
        dispose() {
            renderer.dispose();
            URL.revokeObjectURL(workerUrl);
        }
    };
}
