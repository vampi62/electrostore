# Architecture

## Diagram

> TODO: insert the service diagram here once available (e.g. `docs/assets/architecture.svg` or `.png`).
>
> ```markdown
> ![Architecture](../assets/architecture.svg)
> ```

## Services

| Service | Stack | Role | Exposed via |
|---|---|---|---|
| `electrostoreAPI` | ASP.NET Core | Client-facing backend; business rules, auth. **The only service with direct access to the database and the S3/object storage bucket.** | REST (frontend), gRPC server (internal), Kafka producer |
| `electrostoreFRONT` | Vue.js | Web interface | REST calls to `electrostoreAPI` |
| `electrostoreCRON` | ASP.NET Core | Executes scheduled jobs (stock threshold alerts, item movement reports) | Kafka producer/consumer, gRPC client to `electrostoreAPI` |
| `electrostoreNOTIF` | ASP.NET Core | Sends web push and email notifications | Kafka consumer, gRPC client to `electrostoreAPI` |
| `electrostoreWORKER` | ASP.NET Core | Backend execution service: tracks ESP MQTT connection status and manages MQTT broker (mosquitto) logins; reports results back to `electrostoreAPI` for persistence | MQTT client (status only), Kafka consumer, gRPC client to `electrostoreAPI`, Docker API |

## Data access boundary

`electrostoreAPI` is the only service that ever touches the database or the S3 bucket directly (confirmed: `DbContext`/`FileService` usage exists only under `electrostoreAPI/`). `electrostoreCRON`, `electrostoreNOTIF`, and `electrostoreWORKER` never read or write storage themselves — when one of them needs something persisted, it calls `electrostoreAPI` over gRPC and lets the API do the actual read/write. Keep this boundary when adding new backend logic: if a service other than `electrostoreAPI` needs to save something, add/extend a gRPC call to the API rather than giving that service its own DB/S3 access.

Likewise, when an ESP module needs to receive an MQTT command (e.g. light up LEDs), `electrostoreAPI` publishes it to the broker directly (see [MQTT](#mqtt) below) — `electrostoreWORKER` never sends commands to ESP modules, it only listens for their status.

## Inter-service communication

### REST

`electrostoreFRONT` talks to `electrostoreAPI` over REST only. Full reference: [OpenAPI/Swagger](https://vampi62.github.io/electrostore/openapi/).

### gRPC

`electrostoreAPI` hosts gRPC services (`electrostoreAPI/Grpc/Services`, contracts in `electrostoreAPI/Grpc/Protos`: `commands.proto`, `config.proto`, `cronJobs.proto`, `items.proto`, `itemsHistory.proto`, `storeMqtt.proto`, `users.proto`) consumed internally by `electrostoreCRON`, `electrostoreNOTIF`, and `electrostoreWORKER` — used for server-to-server calls that don't need to go through the public REST surface (e.g. reading items/history for a scheduled job).

These `.proto` files already play the same role `openapi/swagger.json` plays for REST: they're the machine-readable contract, consumed directly by the C# gRPC tooling on both sides. No separate spec file is needed. There's just no rendered/browsable reference for them (swagger-ui's equivalent) — if that's ever wanted, `protoc-gen-doc` can generate static HTML from them, published alongside `openapi/`.

`electrostoreWORKER` currently registers three gRPC clients (`Program.cs`): `StoresMqttGrpc` (used by `MqttClientService` to report ESP connection status), `ConfigGrpc` (used by `ConfigCacheService`), and `CommandsGrpc` — registered but **not called anywhere yet**. This last one is the most likely landing spot for the redesigned package-tracking flow once the disabled `tracking-request-*` Kafka topics (see [Kafka](#kafka) below) come back: `electrostoreWORKER` picking up tracking results and reporting them to `electrostoreAPI` over gRPC instead of Kafka.

### Kafka

`electrostoreAPI` and `electrostoreCRON` publish domain events; `electrostoreCRON`, `electrostoreNOTIF`, and `electrostoreWORKER` consume them. Message contracts live in each service's `Kafka/Messages/` folder and must be kept in sync by hand (no shared contract package) — see [Message shapes](#message-shapes) below for what's currently on the wire.

| Topic | Provisioned by generator? | Producer(s) | Consumer(s) | Purpose |
|---|---|---|---|---|
| `cronjob-events` | Yes | `electrostoreAPI` (`CronJobService`) | `electrostoreCRON` (`KafkaCronJobEventsConsumer`) | Push cron job create/update/delete/force-run/force-stop to the scheduler |
| `notification-requests` | Yes | `electrostoreAPI` (`UserService`, `UserPushSubscriptionService`, `AuthService`), `electrostoreCRON` (`StockLowAlertService`, `ItemMovementReportService`) | `electrostoreNOTIF` (`KafkaNotifConsumer`) | Request a push/email notification to be sent |
| `mqtt-user-events` | Yes | `electrostoreAPI` (`StoreService`) | `electrostoreWORKER` (`KafkaMqttUserConsumer`) | Provision/revoke MQTT broker credentials for a store, applied to the mosquitto container via Docker API |
| `tracking-request-add` / `-change` / `-stop` / `-resume` / `-delete` | No | *disabled* | — | Carrier package-tracking lifecycle events. The publishing code in `electrostoreAPI`'s `CommandService` is commented out as of 2026-09 — planned to come back in a future version with a different design. `electrostoreWORKER`'s matching `TrackingResultMessage` class and the generator's topic provisioning were never wired up either; revisit all three together when this feature returns. |

Kafka topics are created with 3 partitions / replication factor 1 by the generator's init script (`docker exec electrostore-kafka ... kafka-topics.sh --create --topic ...` in `docs/generator/js/generators.js`) — adding a topic in code without adding the matching `--create --topic` line there means it only gets auto-created lazily (`KAFKA_AUTO_CREATE_TOPICS_ENABLE=true`) with default settings, not the intended partition count.

### MQTT

- **Commands to ESP modules always go through `electrostoreAPI`, never `electrostoreWORKER`.** E.g. `LedService` publishes directly to the broker on `electrostore/<store_mqtt_name>/leds` to drive the LedStorage LEDs.
- **`electrostoreWORKER` only reads connection status.** `Mqtt/MqttClientService` subscribes to `electrostore/+/status` and, on each message, calls `electrostoreAPI` over gRPC (`StoresMqttGrpc.UpdateStoreMqttStatusAsync`) to persist `is_mqtt_connected_store`/`mqtt_last_seen_store` — it does not talk to ESP modules on their command topics.
- ESP modules (`ledstore/`) are MQTT clients using credentials provisioned through the `mqtt-user-events` flow above.

## Message shapes

Deliberately kept as plain tables here rather than a formal spec (AsyncAPI/JSON Schema) — with 3 active topics and a small team, a generated-docs pipeline would likely cost more to maintain than the drift it prevents. These tables are the manually-maintained source of truth for what's actually on the wire; update them whenever a `Kafka/Messages/*.cs` class changes (same rule as the data model — see [Maintaining this document](#maintaining-this-document)).

**`mqtt-user-events`** — `MqttUserMessage`, identical in `electrostoreAPI` and `electrostoreWORKER`:

| Field | Type | Notes |
|---|---|---|
| `user` | `string?` | MQTT username to create/update |
| `old_user` | `string?` | previous username, set on rename |
| `password` | `string?` | new password |
| `delete` | `bool?` | `true` revokes instead of creating/updating |

**`cronjob-events`** — producer (`electrostoreAPI`'s `CronJobMessage`) wraps the full `ReadCronJobDto`; consumer (`electrostoreCRON`'s `CronJobEventData`) only reads a subset by matching field name. Extra producer fields are silently ignored by the consumer, which is fine as long as nobody expects them to arrive.

| Field | Sent by API | Read by CRON | Notes |
|---|---|---|---|
| `action` | `string` | `string` | `created` \| `updated` \| `deleted` \| `force_run` \| `force_stop` |
| `data.id_cronjob` | `int` | `int` | |
| `data.name_cronjob` | `string` | `string?` | |
| `data.cron_expression_cronjob` | `string` | `string?` | |
| `data.action_cronjob` | `CronJobAction` enum (serializes as `int`) | `int?` | same wire value, different declared type — works, but drifts silently if the enum ever gets a `[JsonConverter(JsonStringEnumConverter)]` |
| `data.params_cronjob` | `string?` | `string?` | |
| `data.is_enabled` | `bool` | `bool` | |
| `data.last_run_at`, `next_run_at`, `status_cronjob`, `last_error_cronjob`, `created_at`, `updated_at` | present | *not read* | sent but unused by the consumer |

**`notification-requests`** — **confirmed drift**: `TemplateValues` is typed differently in every service that touches this topic.

| Field | Type | Notes |
|---|---|---|
| `Types` | `List<string>` | e.g. `["email"]`, `["push"]` |
| `RecipientEmail` | `string?` | |
| `RecipientUserId` | `int?` | |
| `TemplateId` | `string?` | |
| `Language` | `string?` | |
| `TemplateValues` | **`Dictionary<string,string>`** in `electrostoreAPI` · **`Dictionary<string,JsonElement>`** in `electrostoreNOTIF` · **`Dictionary<string,object>`** in `electrostoreCRON` | `electrostoreNOTIF`'s consumer comment says it supports arrays for `{{#each}}` template blocks, which the `string`-typed producer side (`electrostoreAPI`) can't actually send — worth aligning to one shared type (`JsonElement` or `object`) next time this is touched |
| `Subject`, `Title`, `Body` | `string?` | |
| `PushData` | `Dictionary<string,string>?` | |

## Maintaining this document

Update the Kafka topics table, the message-shape tables, and the service table whenever a topic, consumer, message field, or gRPC contract is added, renamed, or removed — grep for `Topic = "` / `topic = "` across `*/Kafka/` to re-derive the topics if unsure it's still accurate.
