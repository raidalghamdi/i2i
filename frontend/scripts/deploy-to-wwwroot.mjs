// Copies the built Angular SPA into the backend's wwwroot so ASP.NET serves it same-origin.
// Zero dependencies (Node built-in fs). Run via `npm run build:backend`, which builds first.
//
// The Arabic build (angular.json `production-ar` config, locale subPath "") emits at the browser
// root, so we mirror dist/frontend/browser/* into wwwroot, wiping stale files first.
import { existsSync, rmSync, mkdirSync, cpSync, readdirSync } from 'node:fs';
import { fileURLToPath } from 'node:url';
import { dirname, resolve } from 'node:path';

const here = dirname(fileURLToPath(import.meta.url));
const src = resolve(here, '..', 'dist', 'frontend', 'browser');
const dest = resolve(here, '..', '..', 'backend', 'src', 'InnovationToImpact.Api', 'wwwroot');

if (!existsSync(src)) {
  console.error(`[deploy-to-wwwroot] Build output not found: ${src}\nRun the build first (npm run build:backend runs it for you).`);
  process.exit(1);
}

rmSync(dest, { recursive: true, force: true });
mkdirSync(dest, { recursive: true });
cpSync(src, dest, { recursive: true });

console.log(`[deploy-to-wwwroot] Copied ${readdirSync(src).length} top-level entries -> ${dest}`);
