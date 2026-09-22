# Recette — electrostoreAPI

See [recette/README.md](README.md) for the format and when to run this.

| Scenario | Preconditions | Steps | Expected result |
|---|---|---|---|
| Create a store and a zone | Logged in as admin | 1. Create a Zone. 2. Create a Store attached to the Zone, with `position_mode_store`. | Store is created, linked to the Zone, visible via `GET /stores/{id}`. |
| Move item stock between boxes | An Item exists with stock in Box A | 1. Move a quantity of the Item from Box A to Box B via the API. | `ItemsBoxs` quantities update correctly for both boxes; an `ItemsHistory` row is created with the correct user, item, box, and quantity. |
| Item threshold triggers low-stock condition | Item has `threshold_min_item` set | 1. Reduce the Item's stock below `threshold_min_item`. | The Item is flagged/returned as below threshold (verify against whatever endpoint/flag the frontend uses to show the alert badge). |
| Create a Command and attach a Carrier | A Carrier exists | 1. Create a Command with `id_carrier` set. 2. Request tracking (`is_tracking_requested`). | Command is created. Package-tracking Kafka publishing (`tracking-request-*`) is currently disabled in code (commented out, planned to return in a future version) — do **not** expect a Kafka message here until that lands; re-enable this check when it does (see [architecture.md](../docs/reference/architecture.md)). |
| Auth: login, refresh, logout | A user account exists | 1. Login. 2. Use the access token. 3. Refresh via refresh token. 4. Logout. | Tokens issued/rotated/revoked correctly; API rejects requests after logout. |
| SSO login (if configured) | An SSO provider is configured | 1. Login via the SSO flow. | User is created/matched by prefix and logged in (see recent "Handle SSO auth methods by prefix" change). |

Add scenarios here as new endpoints/business rules are added, especially anything with side effects on other services (Kafka events, MQTT publishes).
