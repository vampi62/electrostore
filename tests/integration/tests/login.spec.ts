import { expect, test } from "@playwright/test";
import { ADMIN_EMAIL, ADMIN_PASSWORD } from "./env";

// Real browser, real front, real API: no route is intercepted.

test.describe("Login", () => {
	test.beforeEach(async ({ page }) => {
		await page.addInitScript(() => localStorage.setItem("i18nextLng", "en"));
		await page.goto("/login");
	});

	test("displays the login form", async ({ page }) => {
		await expect(page.locator("form")).toBeVisible();
		await expect(page.locator('input[type="email"]')).toBeVisible();
		await expect(page.locator('input[type="password"]')).toBeVisible();
		await expect(page.locator('button[type="submit"]')).toBeVisible();
	});

	test("shows an error for invalid credentials", async ({ page }) => {
		await page.locator('input[type="email"]').fill(ADMIN_EMAIL);
		await page.locator('input[type="password"]').fill("definitely-not-the-password");
		await page.locator('button[type="submit"]').click();

		await expect(page.locator("form")).toContainText("Incorrect credentials");
		await expect(page).toHaveURL(/\/login/);
	});

	test("logs in with the admin account and reaches the inventory", async ({ page }) => {
		await page.locator('input[type="email"]').fill(ADMIN_EMAIL);
		await page.locator('input[type="password"]').fill(ADMIN_PASSWORD);
		await page.locator('button[type="submit"]').click();

		await expect(page).toHaveURL(/\/inventory/);
	});
});
