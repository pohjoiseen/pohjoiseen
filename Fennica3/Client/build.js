const isDev = process.argv.includes('--dev');

// bundle JS
const result = await Bun.build({
    entrypoints: ['./src/main.ts'],
    naming: 'bundle.js',
    outdir: '../wwwroot/js',
    minify: !isDev,
    sourcemap: isDev ? 'inline' : 'external',
});
if (!result.success) {
    throw new AggregateError(result.logs);
}

// manually concatenate css files
const cssFiles = [
    'node_modules/leaflet/dist/leaflet.css',
    'node_modules/glider-js/glider.css',
    './src/style.css',
];
const css = (await Promise.all(cssFiles.map(f => Bun.file(f).text()))).join('\n');
await Bun.write('../wwwroot/css/style.css', css);

console.log('Fennica3-js build successful');
