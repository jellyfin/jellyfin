import { expect, it, vi } from 'vitest';

import { attachSubtitle } from './subtitles';

it('reports a native subtitle load failure so playback can fall back to burn-in', async () => {
    const video = document.createElement('video');
    const onError = vi.fn();
    const attachment = await attachSubtitle(
        video,
        { Codec: 'vtt', DeliveryMethod: 'External' },
        '/subtitle.vtt',
        onError
    );
    const track = video.querySelector('track')!;

    track.dispatchEvent(new Event('error'));
    expect(onError).toHaveBeenCalledOnce();

    attachment?.dispose();
    expect(video.querySelector('track')).toBeNull();
});
