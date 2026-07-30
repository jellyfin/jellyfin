import eslint from '@eslint/js';
import reactHooks from 'eslint-plugin-react-hooks';
import globals from 'globals';
import tseslint from 'typescript-eslint';

const typescriptFiles = [ '**/*.{ts,tsx}' ];

export default tseslint.config(
    {
        ignores: [
            'coverage',
            'dist',
            'node_modules',
            'output',
            'playwright-report',
            'test-results'
        ]
    },
    eslint.configs.recommended,
    ...tseslint.configs.recommended.map(config => ({
        ...config,
        files: typescriptFiles
    })),
    {
        files: typescriptFiles,
        languageOptions: {
            globals: {
                ...globals.browser,
                ...globals.es2022
            }
        },
        plugins: {
            'react-hooks': reactHooks
        },
        rules: {
            ...reactHooks.configs.flat.recommended.rules,
            'react-hooks/preserve-manual-memoization': 'off',
            '@typescript-eslint/consistent-type-definitions': [ 'error', 'interface' ],
            '@typescript-eslint/no-explicit-any': 'error'
        }
    },
    {
        files: [ 'src/pwa/sw.ts' ],
        languageOptions: {
            globals: {
                ...globals.serviceworker
            }
        }
    },
    {
        files: [
            'scripts/**/*.mjs',
            'playwright.config.ts',
            'vite.config.ts',
            '**/*.test.{ts,tsx}',
            'tests/**/*.ts'
        ],
        languageOptions: {
            globals: {
                ...globals.browser,
                ...globals.node
            }
        }
    }
);
