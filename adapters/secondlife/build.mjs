// Build only the Second Life connector. Python is not a player dependency.
import { build } from 'esbuild';
import { inject } from 'postject';
import { copyFile, mkdir, readFile, writeFile, chmod } from 'node:fs/promises';
import { execFileSync } from 'node:child_process';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const root = fileURLToPath(new URL('../../', import.meta.url));
const platform = process.platform === 'win32' ? 'win' : process.platform === 'linux' ? 'linux' : null;
if (!platform || process.arch !== 'x64') throw new Error('Build on Windows x64 or Linux x64');
const work = path.join(root, 'target/secondlife');
const outputArg = process.argv.indexOf('--output');
const output = outputArg < 0 ? path.join(root, 'dist/secondlife') : path.resolve(process.argv[outputArg + 1]);
await mkdir(work, { recursive: true });
await mkdir(output, { recursive: true });
const main = path.join(work, 'main.cjs');
await build({ entryPoints: [path.join(root, 'adapters/secondlife/relay/host_bridge.mjs')], bundle: true,
  platform: 'node', target: 'node22', format: 'cjs', outfile: main });
const blob = path.join(work, 'sea.blob');
const config = path.join(work, 'sea.json');
await writeFile(config, JSON.stringify({ main, output: blob, disableExperimentalSEAWarning: true,
  useSnapshot: false, useCodeCache: false, assets: { LICENSE: path.join(root, 'LICENSE') } }));
execFileSync(process.execPath, ['--experimental-sea-config', config], { stdio: 'inherit' });
const executable = path.join(output, `XRT-SecondLife-${platform}-x64${platform === 'win' ? '.exe' : ''}`);
await copyFile(process.execPath, executable);
await chmod(executable, 0o755);
await inject(executable, 'NODE_SEA_BLOB', await readFile(blob), {
  sentinelFuse: 'NODE_SEA_FUSE_fce680ab2cc467b6e072b8b5df1996b2', overwrite: true,
});
execFileSync(executable, ['--check'], { stdio: 'inherit' });
console.log(executable);
