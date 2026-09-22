# Business glossary

Definitions of the domain concepts used across the code, the UI, and the other reference docs ([architecture](architecture.md), [data model](data-model.md)). Several terms overlap in everyday language (Store/Zone/Box, Item/Equipement, Command) — this page exists to disambiguate them.

| Term | Definition |
|---|---|
| **Zone** | A physical area grouping several Stores (e.g. a room, a workbench). Optional: a Store doesn't have to belong to a Zone. |
| **Store** | A physical storage unit (a drawer cabinet, a shelf unit) placed in a Zone. Has a coordinate grid (`xlength`/`ylength`) and its own MQTT identity, since a Store can be equipped with LedStorage LEDs. |
| **Box** | A subdivision of a Store (a single drawer, or a rectangular area within it), defined by start/end coordinates. Boxes are what actually hold Items or Equipements. |
| **Item** | A consumable/stockable component (e.g. a resistor reference, a connector). Tracked by quantity across Boxes, with a `threshold_min` used to trigger low-stock alerts. |
| **Equipement** | A non-consumable, individually tracked physical asset (e.g. a tool, an instrument) stored in a Box. Unlike an Item, it has its own status and maintenance history rather than a quantity. |
| **Item history / movement** | A logged change in an Item's quantity or location (`ItemsHistory`): who (`User`) moved how much, from/into which `Box`. |
| **Tag** | A free-form label attachable to Stores, Boxes, Items, or Equipements for cross-cutting classification/search. |
| **Project** | An electronics project record (notes, diagrams, history) that Items can be associated with. Has its own status history and its own tag system (`ProjectTags`), separate from the general `Tags` used by Stores/Boxes/Items/Equipements. |
| **Command** | A purchase order (external supplier order), optionally tied to a `Carrier` for delivery tracking. Distinct from a "cron job" (see below) despite the similar English word. |
| **Carrier** | A shipping/delivery company associated with a Command, used for tracking. |
| **Cron job** | A scheduled background task definition consumed by `electrostoreCRON` (e.g. the recurring stock-threshold check) — unrelated to `Command`/orders. |
| **LedStorage** | The ESP-01 + WS2812B hardware module that lights up LEDs on a Store/Box to indicate a component's location. |

## Maintaining this document

Add a term here whenever a new domain concept is introduced that isn't self-evident from its name alone, or when two concepts are easy to confuse (as `Item`/`Equipement` or `Command`/cron job already are).
