# Recette — electrostoreFRONT

See [recette/README.md](README.md) for the format and when to run this.

| Scenario | Preconditions | Steps | Expected result |
|---|---|---|---|
| First login and password change | Default admin account (`admin@localhost.local`) | 1. Log in with default credentials. 2. Change the password as prompted. | Login succeeds; password change is enforced/possible on first login. |
| Browse storage map | At least one Zone/Store/Box with items exists | 1. Open the storage view for a Store. | 2D/3D mapping renders boxes and their content correctly. |
| Search and highlight via LedStorage | A Store has LedStorage configured and MQTT connected | 1. Search for an Item stored in a known Box. 2. Trigger "locate". | Corresponding LEDs light up on the physical Store (cross-check with [electrostoreWORKER.md](electrostoreWORKER.md)). |
| Stock low notification appears | An Item is below its threshold | 1. Log in as a user subscribed to notifications. | Notification panel shows the low-stock alert (cross-check with [electrostoreNOTIF.md](electrostoreNOTIF.md)). |
| Create and browse a Project | — | 1. Create a Project. 2. Attach Items to it. 3. Add a comment. | Project view shows attached items and comment history. |
| Document/datasheet upload | An Item exists | 1. Upload a datasheet to the Item. | Document appears in the Item's document list and is downloadable. |

Add scenarios here for new UI flows, especially ones spanning a hardware round-trip (LedStorage) since those aren't covered by frontend unit tests.
