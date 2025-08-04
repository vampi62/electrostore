import { fileURLToPath, URL } from "node:url";
import { mergeConfig, defineConfig } from "vite";
import { configDefaults } from "vitest/config";
import viteConfig from "../../electrostoreFRONT/vite.config";
import path from "path";

// Get absolute paths
const mainProjectRoot = path.resolve(__dirname, "../../electrostoreFRONT");
const mainProjectSrc = path.resolve(mainProjectRoot, "src");
const mainProjectNodeModules = path.resolve(mainProjectRoot, "node_modules");

export default mergeConfig(
	viteConfig,
	defineConfig({
		test: {
			environment: "jsdom",
			exclude: [...configDefaults.exclude, "e2e/*"],
			root: fileURLToPath(new URL("./", import.meta.url)),
			include: ["./**/*.spec.js"],
			transformMode: {
				web: [/\.[jt]sx$/],
			},
		},
		resolve: {
			alias: {
				"@": mainProjectSrc,
				// Add explicit aliases for key dependencies
				"vue": path.resolve(mainProjectNodeModules, "vue"),
				"pinia": path.resolve(mainProjectNodeModules, "pinia"),
			},
		},
	}),
);