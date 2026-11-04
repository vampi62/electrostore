# Electrostore Tests

This directory contains unit tests for the Electrostore project. The tests are organized by module:

## electrostoreAPI

C# unit tests for the ASP.NET Core API. These tests use xUnit and Moq for testing.

### Running the tests for all backend modules

```bash
cd electrostore<API / CRON / NOTIF / WORKER>
dotnet test ../tests/electrostore<API / CRON / NOTIF / WORKER>
```

### Generate openAPI documentation for electrostoreAPI

```bash
cd electrostoreAPI

set SwaggerGeneration=true
# or
export SwaggerGeneration=true

dotnet swagger tofile --output ../docs/openapi.json bin/Debug/net10.0/electrostoreAPI.dll v1
```

## electrostoreFRONT

Vue.js unit tests for the frontend. These tests use Vitest and Vue Test Utils for testing.

### Running the tests

```bash
cd tests/electrostoreFRONT
npm run test:unit
npm run cypress:run # before start the serveur with `npm run dev` in the electrostoreFRONT directory
```

## Integration tests (deployed instance, nothing mocked)

`tests/integration/` holds the tests run by CI against the preview instance of each pull request (see [deploy/README.md](../deploy/README.md)):

- **Playwright** (`tests/*.spec.ts`): API checks (health + build revision, login, protected endpoints, dependency status) and real-browser login flows against the real front and API.
- **k6** (`k6/smoke.js`): light concurrency smoke test of the API with generous thresholds (error rate, p95 latency). It is a regression gate, not a capacity benchmark: a preview runs on a small shared cluster.

```bash
cd tests/integration
npm ci && npx playwright install chromium
E2E_BASE_URL=https://pr-12.devstore.example.com E2E_API_URL=https://api-pr-12.devstore.example.com npx playwright test
k6 run -e API_URL=https://api-pr-12.devstore.example.com k6/smoke.js
```

Credentials default to the admin the API creates on an empty database (`admin@localhost.local` / `Admin@1234`); override with `E2E_ADMIN_EMAIL` / `E2E_ADMIN_PASSWORD` when the instance was seeded with another account.
The Cypress tests in `tests/electrostoreFRONT` stay mock-based (fast, no backend); the Playwright suite is the one that talks to a real stack.

For functional/end-to-end scenarios (as opposed to the unit tests here), see [recette/](../recette/README.md).
