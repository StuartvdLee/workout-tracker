import tsParser from "@typescript-eslint/parser";

export default [
    {
        ignores: ["node_modules/", "dist/", "build/", "coverage/", "*.min.js"]
    },
    {
        files: ["**/*.{ts,tsx}"],
        languageOptions: {
            parser: tsParser,
            ecmaVersion: "latest",
            sourceType: "module",
            parserOptions: {
                ecmaFeatures: {
                    jsx: true
                }
            }
        },
        rules: {
            "no-unused-vars": "warn"
        }
    }
];
