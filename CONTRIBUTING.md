# Contributing to ElectroStore

## Project layout

Five services, each in its own top-level folder: `electrostoreAPI`, `electrostoreCRON`, `electrostoreNOTIF`, `electrostoreWORKER` (ASP.NET Core), and `electrostoreFRONT` (Vue.js). See [docs/reference/architecture.md](docs/reference/architecture.md) for how they talk to each other, and [docs/reference/data-model.md](docs/reference/data-model.md) for the database schema.

## Setting up a dev environment

Follow [docs/01_installation.md](docs/01_installation.md) (Dev mode) or use the [online configurator](https://vampi62.github.io/electrostore/docs/generator/index.html).

## Running tests

Each backend service has a matching test project in `tests/`, and the frontend has unit + e2e tests in `tests/electrostoreFRONT/`. Commands: [tests/README.md](tests/README.md).

Each service also has its own PR test workflow (`.github/workflows/PRtests<SERVICE>.yml`) — CI runs the same tests as `tests/README.md` describes, scoped to whichever service(s) your PR touches.

## Before opening a PR

- Run the tests for every service you touched (see above).
- If you changed `electrostoreAPI/Models/` (entities or foreign keys), update [docs/reference/data-model.md](docs/reference/data-model.md) in the same PR.
- If you added/removed a Kafka topic, a gRPC contract, or changed how two services talk to each other, update [docs/reference/architecture.md](docs/reference/architecture.md).
- If you introduced a new domain concept, add it to [docs/reference/glossary.md](docs/reference/glossary.md).
- Add or update the relevant scenario in [recette/](recette/README.md) if the change affects user-facing or cross-service behavior.
- If you touched any configuration parameter, see [Keeping the generator in sync](#keeping-the-generator-in-sync) below.

There is no `CHANGELOG.md` to maintain by hand: release notes are auto-generated from commit history by `.github/workflows/release.yml` and published to [GitHub Releases](https://github.com/vampi62/electrostore/releases) — this is why commit messages should stay descriptive (see below).

## Keeping the generator in sync

[docs/generator/](docs/generator/index.html) is the recommended way to deploy the project: it fills in a form and generates `docker-compose.yml`, `appsettings.json` per service, and the `setup.sh`/`setup.ps1` init scripts (which also create Kafka topics and the S3 bucket). It hardcodes a lot of what the services expect — topic names, the S3 bucket, env var names, container names.

If your change adds, renames, or removes any of the following, update `docs/generator/js/` in the **same PR**, not as a follow-up:

| You changed... | Update in the generator |
|---|---|
| An `appsettings.json` key a service reads at startup | `js/generators.js` (the function building that service's `appsettings.json`) |
| A Kafka topic name, or added a new topic | `js/generators.js`: the topic-creation block (`kafka-topics.sh --create --topic ...`, both `setup.sh` and `setup.ps1`) |
| The S3 bucket name/default, or added a new bucket | `js/generators.js`: bucket creation step, and `js/config.js` if a new form field is needed |
| A docker-compose service, port, volume, or env var | `js/generators.js` (compose/env generation) |
| A new configurable option (form field) | `js/config.js` (`collectConfig`) + the corresponding input in `index.html` |

If you add a Kafka topic, also update its message-shape table in [docs/reference/architecture.md](docs/reference/architecture.md#message-shapes) — that's what keeps the `NotificationMessage`-style drift (same field, different type in each service) from creeping back in.

## Commit messages

Short, imperative summary (e.g. `Fix item threshold check on stock update`) — no enforced prefix convention. Keep one logical change per commit where practical.

## Branches

Branch from `main`, open the PR against `main`. No enforced naming convention beyond being descriptive of the change.
