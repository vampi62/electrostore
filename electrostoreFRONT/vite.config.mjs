import { writeFileSync } from "fs";
import { fileURLToPath, URL } from "url";
import { defineConfig } from "vite";
import { VitePWA } from "vite-plugin-pwa";
import vue from "@vitejs/plugin-vue";

const versionPlugin = () => ({
	name: "version-plugin",
	closeBundle() {
		const version = {
			version: Date.now().toString(), // or use a version from package.json
			buildDate: new Date().toISOString(),
		};
		writeFileSync("./dist/version.json", JSON.stringify(version));
		console.log(`Version file created with version: ${version.version}`);
	},
});
// https://vitejs.dev/config/
export default defineConfig({
	plugins: [vue(),
		VitePWA({
			registerType: "autoUpdate",
			strategies: "injectManifest",
			srcDir: "src",
			filename: "sw.js",
			includeAssets: ["favicon.ico", "apple-touch-icon.png", "pwa-192x192.png", "pwa-512x512.png"],
			manifest: {
				name: "ElectroStore",
				short_name: "ElectroStore",
				description: "manage your electronic storage",
				theme_color: "#ffffff",
				background_color: "#ffffff",
				display: "standalone",
				start_url: "/",
				icons: [
					{
						src: "pwa/android-192x192.png",
						sizes: "192x192",
						type: "image/png",
					},
					{
						src: "pwa/android-512x512.png",
						sizes: "512x512",
						type: "image/png",
					},
					{
						src: "pwa/android-512x512.png",
						sizes: "512x512",
						type: "image/png",
						purpose: "any maskable",
					},
					{
						src: "pwa/ios-180.png",
						sizes: "180x180",
						type: "image/png",
					},
				],
			},
			injectManifest: {
				globPatterns: ["**/*.{js,css,html,ico,png,svg,woff2}"],
			},
			devOptions: {
				enabled: true,
				type: "module",
			},
		}),
		versionPlugin(),
	],
	resolve: {
		alias: {
			"@": fileURLToPath(new URL("./src", import.meta.url)),
		},
	},
	build: {
		rollupOptions: {
			output: {
				manualChunks(id) {
					if (id.includes("node_modules")) {
						return id
							.toString()
							.split("node_modules/")[1]
							.split("/")[0]
							.toString();
					}
				},
				entryFileNames: "assets/js/[name]-[hash].js",
				chunkFileNames: "assets/js/[name]-[hash].js",
				assetFileNames: "assets/[ext]/[name]-[hash].[ext]",
			},
		},
	},
});
