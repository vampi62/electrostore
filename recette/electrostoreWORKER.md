# Recette — electrostoreWORKER

See [recette/README.md](README.md) for the format and when to run this.

| Scenario | Preconditions | Steps | Expected result |
|---|---|---|---|
| MQTT credentials provisioned for a new Store | Mosquitto broker running | 1. Create a Store via `electrostoreAPI` (triggers `mqtt-user-events`). | The Store's MQTT credentials are created on the mosquitto container (check via `MqttClientService`/Docker); the Store connects successfully. |
| MQTT credentials revoked on Store deletion | A Store with active MQTT credentials exists | 1. Delete the Store. | Credentials are removed from mosquitto; the physical module can no longer authenticate. |
| LedStorage lights up on demand | A Store with LedStorage is MQTT-connected | 1. Trigger a "locate item" action (from `electrostoreAPI`'s `LedService`, topic `electrostore/<store>/leds`). | Correct LEDs light up on the physical module within a reasonable delay. |
| ESP module reconnect after network drop | A Store's ESP module is connected | 1. Power-cycle or disconnect the module's network. 2. Reconnect. | `mqtt_last_seen_store` / connection status updates correctly; module resumes normal operation. |
| Worker restart doesn't lose broker state | Broker and worker both running | 1. Restart `electrostoreWORKER`. | Existing MQTT users/ACLs on the broker are unaffected; worker resumes consuming `mqtt-user-events` from where it left off (or reconciles state on boot — verify actual behavior). |

Add scenarios here for any new MQTT topic or Docker-managed broker interaction.
