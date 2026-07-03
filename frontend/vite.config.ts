import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Proxy /api to the API service when Aspire provides its address. Left unset for
// standalone `vite preview` (e.g. Playwright e2e), where the app uses its fallback.
const apiTarget = process.env.APISERVICE_HTTPS || process.env.APISERVICE_HTTP;

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  server: apiTarget
    ? { proxy: { '/api': { target: apiTarget, changeOrigin: true } } }
    : {},
});
