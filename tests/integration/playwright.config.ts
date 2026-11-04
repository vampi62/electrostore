import { defineConfig, devices } from "@playwright/test";

// The target instance is chosen with environment variables (no server is started here):
//   E2E_BASE_URL            front URL,  e.g. https://pr-12.devstore.example.com
//   E2E_API_URL             API URL,    e.g. https://api-pr-12.devstore.example.com   (without /api)
//   E2E_ADMIN_EMAIL         default admin@localhost.local   (account created by the API on an empty database)
//   E2E_ADMIN_PASSWORD      default Admin@1234
//   E2E_EXPECTED_REVISION   optional: fail fast if /health reports another build
const baseURL = process.env.E2E_BASE_URL ?? "http://localhost:8080";

export default defineConfig({
	testDir: "./tests",
	// Tests share one database: keep them independent but not parallel inside a file.
	fullyParallel: false,
	workers: process.env.CI ? 2 : undefined,
	forbidOnly: !!process.env.CI,
	retries: process.env.CI ? 1 : 0,
	timeout: 60_000,
	expect: { timeout: 15_000 },
	reporter: process.env.CI
		? [["list"], ["html", { open: "never" }], ["github"]]
		: [["list"], ["html", { open: "never" }]],
	use: {
		baseURL,
		trace: "on-first-retry",
		screenshot: "only-on-failure",
		video: "retain-on-failure",
		// The app picks its language from the browser: pin it so that assertions on texts are stable.
		locale: "en-US",
	},
	projects: [
		{ name: "chromium", use: { ...devices["Desktop Chrome"] } },
	],
});
