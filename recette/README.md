# Recette

Functional / user-acceptance test scenarios, one file per service. Distinct from `tests/` (automated unit and integration tests run in CI): these are manual (or manually-triggered) checklists used to validate a release or a significant change against real user-facing behavior, including cross-service flows that are hard to cover with unit tests (MQTT round-trips, Kafka event chains, notifications actually being received).

## When to use this

- Before tagging a release.
- After a change that touches more than one service (e.g. a Kafka topic, a gRPC contract).
- After a change to hardware-facing behavior (LedStorage).

## How to run a recette pass

1. Deploy the target version (see [docs/01_installation.md](../docs/01_installation.md)).
2. Go through the relevant file(s) below, ticking each scenario.
3. Log any failure as a GitHub issue, referencing the scenario.

## Files

| File | Covers |
|---|---|
| [electrostoreAPI.md](electrostoreAPI.md) | REST endpoints, business rules, auth |
| [electrostoreFRONT.md](electrostoreFRONT.md) | End-user web interface flows |
| [electrostoreWORKER.md](electrostoreWORKER.md) | MQTT / ESP module interactions |
| [electrostoreCRON.md](electrostoreCRON.md) | Scheduled jobs and their side effects |
| [electrostoreNOTIF.md](electrostoreNOTIF.md) | Push and email notification delivery |

## Scenario format

Each scenario is a row in a table:

| Field | Meaning |
|---|---|
| Scenario | Short name |
| Preconditions | State required before starting |
| Steps | Numbered actions |
| Expected result | Observable outcome that makes the scenario pass |

## Maintaining this document

Add a scenario here whenever a bug is found that unit tests didn't catch because it only shows up end-to-end — that's the signal this checklist is missing something.
