# Recette — electrostoreNOTIF

See [recette/README.md](README.md) for the format and when to run this.

| Scenario | Preconditions | Steps | Expected result |
|---|---|---|---|
| Web push notification delivered | A user has an active `UserPushSubscriptions` entry, browser notifications allowed | 1. Trigger a `notification-requests` event (e.g. via the stock-low scenario in [electrostoreCRON.md](electrostoreCRON.md)). | Push notification appears on the subscribed device within a reasonable delay. |
| Email notification delivered | A user has notification-by-email enabled, mail server reachable | 1. Trigger a `notification-requests` event for that user. | Email is received with correct template/content. |
| Notification template renders correctly | — | 1. Trigger each notification type at least once (stock low, item movement report, auth-related, etc.). | Each template renders without missing placeholders/broken formatting, for both push and email. |
| Expired/invalid push subscription is handled gracefully | A `UserPushSubscriptions` entry is stale/revoked by the browser | 1. Trigger a notification for that user. | Service logs/handles the send failure without crashing; ideally cleans up or flags the stale subscription. |
| Service restart doesn't drop in-flight notifications | Kafka has pending `notification-requests` messages | 1. Restart `electrostoreNOTIF` while messages are queued. | Messages are still consumed after restart (consumer group offset behavior as expected), no silent loss. |

Add scenarios here for any new notification type or delivery channel.
