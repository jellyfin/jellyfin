import type { MediaStream } from './types';

export interface AttachedSubtitle {
    dispose(): void;
}

export async function attachSubtitle(
    video: HTMLVideoElement,
    stream: MediaStream | undefined,
    subtitleUrl: string | undefined,
    onError: () => void
): Promise<AttachedSubtitle | null> {
    if (!stream || !subtitleUrl) return null;
    const codec = stream.Codec?.toLowerCase() ?? '';

    try {
        if (codec === 'ass' || codec === 'ssa') {
            const { createAssRenderer } = await import('./assRenderer');
            return createAssRenderer(video, subtitleUrl, onError);
        }
        if (codec === 'pgssub' || codec === 'pgs') {
            const { createPgsRenderer } = await import('./pgsRenderer');
            return await createPgsRenderer(video, subtitleUrl);
        }

        const track = document.createElement('track');
        track.kind = 'subtitles';
        track.label = stream.DisplayTitle || stream.Title || stream.Language || 'Subtitles';
        track.srclang = stream.Language || '';
        track.src = subtitleUrl;
        track.default = true;
        track.addEventListener('load', () => {
            track.track.mode = 'showing';
        }, { once: true });
        track.addEventListener('error', onError, { once: true });
        video.appendChild(track);
        return { dispose: () => track.remove() };
    } catch {
        onError();
        return null;
    }
}
