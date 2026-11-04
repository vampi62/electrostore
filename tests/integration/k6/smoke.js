// k6 smoke / light performance gate for the API of a deployed instance.
//
//   k6 run -e API_URL=https://api-pr-12.devstore.example.com \
//          -e ADMIN_EMAIL=admin@localhost.local -e ADMIN_PASSWORD='Admin@1234' tests/integration/k6/smoke.js
//
// This is NOT a capacity test: a preview instance runs on a small shared cluster. It catches
// regressions in behaviour under (light) concurrency - errors, deadlocks, N+1 queries that make
// a list endpoint slow - using generous thresholds.
import http from "k6/http";
import { check, sleep } from "k6";

const API_URL = (__ENV.API_URL || "http://localhost:5000").replace(/\/+$/, "");
const ADMIN_EMAIL = __ENV.ADMIN_EMAIL || "admin@localhost.local";
const ADMIN_PASSWORD = __ENV.ADMIN_PASSWORD || "Admin@1234";

export const options = {
	scenarios: {
		browse: {
			executor: "constant-vus",
			vus: Number(__ENV.VUS || 5),
			duration: __ENV.DURATION || "30s",
		},
	},
	thresholds: {
		http_req_failed: ["rate<0.01"],
		http_req_duration: ["p(95)<1500"],
		checks: ["rate>0.99"],
	},
};

export function setup() {
	const res = http.post(
		`${API_URL}/api/auth/login`,
		JSON.stringify({ email: ADMIN_EMAIL, password: ADMIN_PASSWORD }),
		{ headers: { "Content-Type": "application/json" } },
	);
	check(res, { "login succeeded": (r) => r.status === 200 });
	return { token: res.json("token") };
}

export default function (data) {
	const params = { headers: { Authorization: `Bearer ${data.token}` } };

	for (const path of ["/api/item", "/api/tag", "/api/store", "/api/config"]) {
		const res = http.get(`${API_URL}${path}`, params);
		check(res, { [`GET ${path} is 200`]: (r) => r.status === 200 });
	}

	check(http.get(`${API_URL}/health`), { "health is 200": (r) => r.status === 200 });
	sleep(1);
}
