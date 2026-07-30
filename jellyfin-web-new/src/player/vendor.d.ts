declare module '@jellyfin/libass-wasm' {
    interface Options {
        video: HTMLVideoElement;
        subUrl: string;
        workerUrl: string;
        fallbackFont?: string;
        onError?: (error: unknown) => void;
        renderMode?: string;
        libassMemoryLimit?: number;
        libassGlyphLimit?: number;
        prescaleFactor?: number;
        prescaleHeightLimit?: number;
        maxRenderHeight?: number;
        renderAhead?: number;
    }

    export default class SubtitlesOctopus {
        constructor(options: Options);
        dispose(): void;
    }
}

declare module 'libpgs' {
    export class PgsRenderer {
        constructor(options: {
            video: HTMLVideoElement;
            subUrl: string;
            aspectRatio: 'contain';
            workerUrl: string;
        });
        dispose(): void;
    }
}
