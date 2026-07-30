import workerUrl from 'libpgs/dist/libpgs.worker.js?url';

export async function createPgsRenderer(video: HTMLVideoElement, subtitleUrl: string) {
    const { PgsRenderer } = await import('libpgs');
    const renderer = new PgsRenderer({
        video,
        subUrl: subtitleUrl,
        aspectRatio: 'contain',
        workerUrl
    });
    return { dispose: () => renderer.dispose() };
}
