// Target instance, see playwright.config.ts.
export const API_URL = (process.env.E2E_API_URL ?? "http://localhost:5000").replace(/\/+$/, "");
export const ADMIN_EMAIL = process.env.E2E_ADMIN_EMAIL || "admin@localhost.local";
export const ADMIN_PASSWORD = process.env.E2E_ADMIN_PASSWORD || "Admin@1234";
export const EXPECTED_REVISION = process.env.E2E_EXPECTED_REVISION || "";
