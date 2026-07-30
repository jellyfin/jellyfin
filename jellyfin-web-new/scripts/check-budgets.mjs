import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { gzipSync } from 'node:zlib';

const dist = resolve(import.meta.dirname, '..', 'dist');
const html = readFileSync(resolve(dist, 'index.html'), 'utf8');
const assets = [ ...html.matchAll(/(?:src|href)="\.\/([^"]+\.(?:css|js))"/g) ]
    .map(match => match[1])
    .filter((value, index, values) => values.indexOf(value) === index);

const sizes = assets.reduce((totals, asset) => {
    const type = asset.endsWith('.css') ? 'css' : 'js';
    totals[type] += gzipSync(readFileSync(resolve(dist, asset))).byteLength;
    return totals;
}, { css: 0, js: 0 });

const budgets = { css: 50 * 1024, js: 300 * 1024 };
for (const type of [ 'js', 'css' ]) {
    const kib = (sizes[type] / 1024).toFixed(1);
    console.log(`${type.toUpperCase()} initial : ${kib} Kio gzip`);
    if (sizes[type] > budgets[type]) {
        throw new Error(`Budget ${type.toUpperCase()} dépassé (${kib} Kio)`);
    }
}
