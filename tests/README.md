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

dotnet swagger tofile --output ../docs/openapi.json bin/Debug/net9.0/electrostoreAPI.dll v1
```

## electrostoreFRONT

Vue.js unit tests for the frontend. These tests use Vitest and Vue Test Utils for testing.

### Running the tests

```bash
cd tests/electrostoreFRONT
npm run test:unit
npm run cypress:run # before start the serveur with `npm run dev` in the electrostoreFRONT directory
```

For functional/end-to-end scenarios (as opposed to the unit tests here), see [recette/](../recette/README.md).
