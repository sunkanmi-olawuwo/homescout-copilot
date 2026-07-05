import js from '@eslint/js';
import { defineConfig, globalIgnores } from 'eslint/config';
import globals from 'globals';
import reactHooks from 'eslint-plugin-react-hooks';
import reactRefresh from 'eslint-plugin-react-refresh';
import tseslint from 'typescript-eslint';

export default defineConfig(
  globalIgnores(['dist']),
  {
    extends: [js.configs.recommended, tseslint.configs.recommended],
    files: ['**/*.{ts,tsx}'],
    languageOptions: {
      globals: globals.browser,
    },
    plugins: {
      'react-hooks': reactHooks,
      'react-refresh': reactRefresh,
    },
    rules: {
      ...reactHooks.configs.recommended.rules,
      'react-refresh/only-export-components': [
        'warn',
        { allowConstantExport: true },
      ],
      // Advisory complexity budget. Warnings (not errors) so they surface in `pnpm run lint`
      // without failing the gate; tighten to 'error' once the baseline sits comfortably under.
      // Cyclomatic complexity, nesting depth and parameter count are the meaningful signals;
      // raw line count is deliberately not enforced — it is a poor proxy for verbose-but-simple
      // JSX view components and test bodies (see wiki/static-analysis.md).
      complexity: ['warn', 15],
      'max-depth': ['warn', 4],
      'max-params': ['warn', 5],
    },
  },
);
