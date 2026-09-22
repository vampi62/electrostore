# Recette — electrostoreCRON

See [recette/README.md](README.md) for the format and when to run this.

| Scenario | Preconditions | Steps | Expected result |
|---|---|---|---|
| Stock low alert fires on schedule | An Item is below `threshold_min_item`; the low-stock cron job is enabled | 1. Wait for (or trigger) the scheduled run. | A `notification-requests` Kafka message is published for the Item (cross-check delivery in [electrostoreNOTIF.md](electrostoreNOTIF.md)). |
| Item movement report generated on schedule | Some `ItemsHistory` entries exist in the reporting window | 1. Wait for (or trigger) the scheduled run. | Report is generated/sent covering the expected movements, no duplicates across consecutive runs. |
| New cron job registered via API is picked up | — | 1. Create a `CronJob` via `electrostoreAPI` (publishes `cronjob-events`). | `electrostoreCRON` starts scheduling the new job without a restart. |
| Cron job updated/deleted via API is applied | An existing `CronJob` is scheduled | 1. Update or delete it via `electrostoreAPI`. | The running schedule reflects the change (new interval honored, or job stops firing) without a restart. |
| Scheduler survives a restart | Jobs are scheduled | 1. Restart `electrostoreCRON`. | All previously registered jobs are reloaded and resume on schedule; no duplicate firings. |

Add scenarios here for any new scheduled job or any new event this service reacts to.
