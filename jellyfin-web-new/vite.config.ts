import react from '@vitejs/plugin-react';
import { defineConfig, loadEnv } from 'vite';
import { VitePWA } from 'vite-plugin-pwa';

export default defineConfig(({ mode }) => {
    const env = loadEnv(mode, process.cwd(), '');
    const jellyfinTarget = env.JELLYFIN_DEV_SERVER || 'http://127.0.0.1:8096';

    return {
        base: './',
        plugins: [
            react(),
            VitePWA({
                strategies: 'injectManifest',
                srcDir: 'src/pwa',
                filename: 'sw.ts',
                registerType: 'prompt',
                injectRegister: false,
                manifest: {
                    name: 'Jellyfin',
                    short_name: 'Jellyfin',
                    description: 'Films et séries, simplement.',
                    start_url: './#/home',
                    scope: './',
                    display: 'standalone',
                    theme_color: '#000000',
                    background_color: '#000000',
                    icons: [
                        {
                            src: 'icons/icon-192.svg',
                            sizes: '192x192',
                            type: 'image/svg+xml',
                            purpose: 'any'
                        },
                        {
                            src: 'icons/icon-512.svg',
                            sizes: '512x512',
                            type: 'image/svg+xml',
                            purpose: 'any maskable'
                        }
                    ]
                },
                injectManifest: {
                    globIgnores: [
                        '**/assRenderer.*.js',
                        '**/hls.*.js',
                        '**/libpgs.*.js',
                        '**/pgsRenderer.*.js'
                    ],
                    globPatterns: [ '**/*.{html,js,css,woff2,svg,png,ico,json}' ],
                    manifestTransforms: [ entries => ({
                        manifest: entries.filter(({ url }) =>
                            !/(?:assRenderer|hls|libpgs|pgsRenderer)\.[^/]+\.js$/i.test(url)),
                        warnings: []
                    }) ],
                    maximumFileSizeToCacheInBytes: 2 * 1024 * 1024
                }
            })
        ],
        build: {
            target: [ 'chrome111', 'edge111', 'firefox114', 'safari16.4' ],
            sourcemap: true,
            cssCodeSplit: true,
            rollupOptions: {
                output: {
                    entryFileNames: 'assets/[name].[hash].js',
                    chunkFileNames: 'assets/[name].[hash].js',
                    assetFileNames: 'assets/[name].[hash][extname]'
                }
            }
        },
        server: {
            port: 5173,
            strictPort: true,
            proxy: {
                '/jellyfin-api': {
                    target: jellyfinTarget,
                    changeOrigin: true,
                    ws: true,
                    rewrite: path => path.replace(/^\/jellyfin-api/, '')
                }
            }
        },
        test: {
            environment: 'jsdom',
            include: [ 'src/**/*.test.{ts,tsx}' ],
            setupFiles: [ './src/test/setup.ts' ],
            restoreMocks: true,
            coverage: {
                provider: 'v8',
                reporter: [ 'text', 'html' ],
                include: [ 'src/**/*.{ts,tsx}' ],
                exclude: [ 'src/main.tsx', 'src/pwa/sw.ts' ]
            }
        }
    };
});
