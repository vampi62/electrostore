import js from "@eslint/js";
import globals from "globals";
import pluginVue from "eslint-plugin-vue";
import tseslint from "typescript-eslint";

export default [
	{
		ignores: [
			"dist/**",
			"dev-dist/**",
			"dist-ssr/**",
			"coverage/**",
			"node_modules/**",
			"tailwind.config.mjs",
			"postcss.config.mjs",
		],
	},
	js.configs.recommended,
	...tseslint.configs.recommended.map((config) => ({ ...config, files: ["**/*.ts"] })),
	...pluginVue.configs["flat/essential"],
	{
		files: ["**/*.vue"],
		languageOptions: {
			parserOptions: {
				parser: tseslint.parser,
			},
		},
	},
	{
		languageOptions: {
			ecmaVersion: 2022,
			sourceType: "module",
			globals: {
				...globals.browser,
				...globals.node,
			},
		},
		rules: {
			"no-constant-condition": ["error", { checkLoops: false }],
			"no-unused-vars": 0,
			"no-const-assign": 0,
			"vue/multi-word-component-names": 0,
			quotes: ["error", "double"],
			indent: ["error", "tab"],
			semi: ["error", "always"],
			"comma-dangle": ["error", "always-multiline"],
			"arrow-parens": ["error", "always"],
			"no-console": "warn",
			eqeqeq: ["error", "always"],
			curly: ["error", "all"],
			"brace-style": ["error", "1tbs"],
			"space-before-function-paren": ["error", "never"],
			"keyword-spacing": ["error", { before: true, after: true }],
			"space-infix-ops": "error",
			"padded-blocks": ["error", "never"],
			"no-multiple-empty-lines": ["error", { max: 1, maxEOF: 0 }],
			"object-curly-spacing": ["error", "always"],
			"array-bracket-spacing": ["error", "never"],
			"vue/no-mutating-props": 0,
			"vue/no-reserved-component-names": 0,
		},
	},
];
