import { expect, test } from "@playwright/test";
import { ADMIN_EMAIL, ADMIN_PASSWORD, API_URL, EXPECTED_REVISION } from "./env";

// Talks to the deployed API and everything behind it (MariaDB, Kafka, MQTT, S3): nothing is mocked.

test.describe("API", () => {
	test("health endpoint reports a healthy API (and the expected build)", async ({ request }) => {
		const response = await request.get(`${API_URL}/health`);
		expect(response.ok()).toBeTruthy();
		const body = await response.json();
		expect(body.status).toBe("healthy");
		if (EXPECTED_REVISION) {
			expect(body.revision).toBe(EXPECTED_REVISION);
		}
	});

	test("status endpoint reports every dependency up", async ({ request }) => {
		const login = await request.post(`${API_URL}/api/auth/login`, {
			data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
		});
		expect(login.ok()).toBeTruthy();
		const { token } = await login.json();

		const response = await request.get(`${API_URL}/api/status`, {
			headers: { Authorization: `Bearer ${token}` },
		});
		expect(response.ok()).toBeTruthy();
		const status = await response.json();
		expect(status.api_status).toBe("healthy");
		expect(status.db_connected).toBe(true);
		expect(status.mqtt_connected).toBe(true);
		expect(status.kafka_connected).toBe(true);
		expect(status.notif_status).toBe("healthy");
		expect(status.cron_status).toBe("healthy");
		expect(status.worker_status).toBe("healthy");
	});

	test("rejects wrong credentials", async ({ request }) => {
		const response = await request.post(`${API_URL}/api/auth/login`, {
			data: { email: ADMIN_EMAIL, password: "definitely-not-the-password" },
		});
		expect(response.status()).toBe(401);
	});

	test("protected endpoints require a token and work with one", async ({ request }) => {
		const anonymous = await request.get(`${API_URL}/api/item`);
		expect(anonymous.status()).toBe(401);

		const login = await request.post(`${API_URL}/api/auth/login`, {
			data: { email: ADMIN_EMAIL, password: ADMIN_PASSWORD },
		});
		expect(login.ok()).toBeTruthy();
		const { token } = await login.json();

		const items = await request.get(`${API_URL}/api/item`, {
			headers: { Authorization: `Bearer ${token}` },
		});
		expect(items.ok()).toBeTruthy();
		expect(items.headers()["x-total-count"]).toBeDefined();
	});
});
