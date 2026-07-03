import { defineConfig } from 'vitest/config';
import react from '@vitejs/plugin-react';

// Unit/component tests run under jsdom. Kept separate from vite.config.ts so the
// build config stays free of test concerns.
export default defineConfig({
  plugins: [react()],
  test: {
    environment: 'jsdom',
    globals: false,
    include: ['src/**/*.test.{ts,tsx}'],
  },
});
