interface RuntimeConfig {
  production?: boolean;
  demoMode?: boolean;
  apiUrl?: string;
  supabaseUrl?: string;
  supabasePublishableKey?: string;
}

declare global {
  interface Window { __BH_CONFIG__?: RuntimeConfig; }
}

const runtime = window.__BH_CONFIG__ ?? {};

export const environment = {
  production: runtime.production ?? false,
  demoMode: runtime.demoMode ?? false,
  apiUrl: (runtime.apiUrl ?? 'http://localhost:5207/api').replace(/\/$/, ''),
  supabaseUrl: (runtime.supabaseUrl ?? 'https://oigcjnymanhasdhrktpq.supabase.co').replace(/\/$/, ''),
  supabasePublishableKey: runtime.supabasePublishableKey ?? 'sb_publishable_rGzM07Svs2esjJRCdKRSJQ_MwfhFCRF',
};
