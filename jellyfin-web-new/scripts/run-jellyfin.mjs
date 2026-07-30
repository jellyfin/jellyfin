import { existsSync } from 'node:fs';
import { spawn } from 'node:child_process';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const dist = resolve(root, 'dist');
const project = resolve(root, '..', 'Jellyfin.Server');

if (!existsSync(resolve(dist, 'index.html'))) {
    console.error('dist/index.html est absent. Lancez d’abord « npm run build ».');
    process.exit(1);
}

const server = spawn('dotnet', [
    'run',
    '--project',
    project,
    '--',
    '--webdir',
    dist,
    '--',
    "--ffmpeg",
    // C:\\jellyfin-ffmpeg\\ffmpeg.exe
    resolve(fileURLToPath('file:///C:/jellyfin-ffmpeg/ffmpeg.exe'))
], {
    cwd: root,
    shell: false,
    stdio: 'inherit'
});

server.on('error', error => {
    console.error(`Impossible de démarrer Jellyfin : ${error.message}`);
    process.exitCode = 1;
});

server.on('exit', code => {
    process.exitCode = code ?? 1;
});

for (const signal of [ 'SIGINT', 'SIGTERM' ]) {
    process.on(signal, () => server.kill(signal));
}
