import { mkdir, writeFile } from 'node:fs/promises';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(root, 'public', 'config.js');
const production = process.env.BH_PRODUCTION === 'true' || process.env.VERCEL_ENV === 'production';
const config = {
  production,
  demoMode: process.env.BH_DEMO_MODE === 'true',
  apiUrl: process.env.BH_API_URL || 'http://localhost:5207/api',
  supabaseUrl: process.env.BH_SUPABASE_URL || 'https://oigcjnymanhasdhrktpq.supabase.co',
  supabasePublishableKey: process.env.BH_SUPABASE_PUBLISHABLE_KEY || 'sb_publishable_rGzM07Svs2esjJRCdKRSJQ_MwfhFCRF',
};

await mkdir(dirname(output), { recursive: true });
await writeFile(output, `window.__BH_CONFIG__ = ${JSON.stringify(config, null, 2)};\n`, 'utf8');
console.log(`Configuración ${production ? 'productiva' : 'local'} generada en public/config.js.`);
