# electrostore

## Overview

ElectroStore is a comprehensive electronic component inventory management solution (adaptable to any type of object), combining a web interface, embedded ESP modules, and a microservices architecture.

<!-- SCREENSHOT: general view of the web interface (dashboard) -->
<!-- ![Dashboard](docs/assets/screenshot_dashboard.png) -->

### Main Features

#### Component Inventory
Manage your storage space inventory: reference your components, define minimum quantities, and track your stock levels in real time.

<!-- SCREENSHOT: component list view with quantities -->
<!-- ![Inventory](docs/assets/screenshot_inventory.png) -->

#### Visual Camera Search (ScanBox)
A case equipped with an ESP32CAM camera allows you to photograph a component and automatically identify it using an integrated artificial intelligence module.

<!-- SCREENSHOT: scan interface and recognition result -->
<!-- ![ScanBox](docs/assets/screenshot_scanbox.png) -->

#### Location LEDs (LedStorage)
WS2812B LED strips mounted on your storage spaces light up to visually indicate the location or destination of a component during a search or pickup.

<!-- SCREENSHOT: cabinet with LEDs lit indicating a drawer -->
<!-- ![LedStorage](docs/assets/screenshot_led_storage.png) -->

#### Storage Space Computer View
Visualize in real time the mapping of your storage spaces (boxes, drawers, shelves) and their contents directly from the web interface.

<!-- SCREENSHOT: 2D/3D view of storage space -->
<!-- ![Storage view](docs/assets/screenshot_storage_map.png) -->

#### Replenishment Order Tracking
Create and track your supplier orders, integrate tracking numbers (via 17Track), and receive alerts at each delivery stage.

<!-- SCREENSHOT: order list view with status -->
<!-- ![Orders](docs/assets/screenshot_orders.png) -->

#### Stock Notifications and Alerts
Receive notifications (web push, email) when stock falls below the minimum threshold or during important events (delivery received, component depleted, etc.).

<!-- SCREENSHOT: notifications panel -->
<!-- ![Notifications](docs/assets/screenshot_notifications.png) -->

#### Electronic Project Management
Store your electronics projects: diagrams, ideas, design notes, exchanges, and history. Associate your components directly with a project for easier management.

<!-- SCREENSHOT: project view with associated components -->
<!-- ![Projects](docs/assets/screenshot_projects.png) -->

#### Documents, Datasheets, and AI Assistance (RAG)
Import and organize your technical datasheets and documents associated with your components or projects. These documents can be integrated into a RAG LLM pipeline to assist you with your electronics projects.

<!-- SCREENSHOT: documents library view -->
<!-- ![Documents](docs/assets/screenshot_documents.png) -->

---

## Technical Architecture

ElectroStore consists of several services:

| Service | Role |
|---|---|
| **API** | REST + gRPC backend, database management |
| **IA** | Image recognition (ESP32CAM), ML models |
| **NOTIF** | Push and email notifications |
| **CRON** | Scheduled tasks (stock alerts, order tracking) |
| **WORKER** | MQTT communication with ESP modules |
| **FRONT** | Vue.js web interface |

API Documentation:  
[https://vampi62.github.io/electrostore/openapi/](https://vampi62.github.io/electrostore/openapi/)

---

## Installation

> **Recommended**: Use the **online configurator** to automatically generate all necessary files (docker-compose, configuration files, setup.sh installation script) according to the desired deployment type.
>
> **[Open the configurator](https://vampi62.github.io/electrostore/docs/generator/index.html)**

The configurator supports three deployment types:

| Type | Description |
|---|---|
| **Release** | Stable version recommended for production, uses Docker images published on GHCR |
| **Latest** | Development version, uses Docker images published on GHCR from the `main` branch (features under development) |
| **Dev** | Local development version, builds Docker images from source code on your machine |

It generates for you:
- Complete `docker-compose.yml` with all services (can include optional services such as MariaDB, MQTT, Garage)
- `appsettings.json` files for each service
- Ready-to-use `setup.sh` script to deploy with a single command

<!-- SCREENSHOT: online configurator interface -->
<!-- ![Configurator](docs/assets/screenshot_configurator.png) -->

For manual installation or detailed information about each service, see the complete guide:  
[docs/01_installation.md](docs/01_installation.md)

---

## Required Hardware

### ScanBox (ESP32CAM)

The scan case allows visual identification of a component by photography.

<!-- SCREENSHOT or PHOTO: printed and assembled ScanBox case -->
<!-- ![Assembled ScanBox](docs/assets/photo_scanbox.jpg) -->

3D files and Arduino code are located in the `scanbox/` folder.  
Print preferably in white for better object detection.

| Quantity | Component |
|---|---|
| 1 | ESP32CAM |
| 1 | WS2812B |
| 1 | ON/OFF Switch |

Wiring details and configuration procedure: [docs/02_storeLed_and_scanner.md](docs/02_storeLed_and_scanner.md)

---

### LedStorage (ESP-01)

The LED module installs at the back of your storage space and lights up the LEDs corresponding to a component's location.

<!-- SCREENSHOT or PHOTO: LedStorage module mounted on a drawer cabinet -->
<!-- ![Mounted LedStorage](docs/assets/photo_led_storage.jpg) -->

3D files and Arduino code are located in the `ledstore/` folder.  
Compatible with Parkside drawer cabinets from Lidl:  
https://www.lidl.fr/p/parkside-casiers-a-tiroirs/p100377898

| Quantity | Component |
|---|---|
| 1 | ESP-01 (ESP8266) |
| X | WS2812B |
| 1 | WS2812 LED controller for ESP01 |

Wiring details and configuration procedure: [docs/02_storeLed_and_scanner.md](docs/02_storeLed_and_scanner.md)

---

## Development

To install and contribute to the development version, clone the repository and use the configurator in **Dev** mode, or follow the detailed installation guide.

```bash
git clone https://github.com/vampi62/electrostore
cd electrostore
```

Prerequisites:
- Docker
- (recommended) Traefik with Docker provider and Let's Encrypt
- MariaDB server
- MQTT server

For detailed step-by-step instructions for the dev version: [docs/01_installation.md](docs/01_installation.md)

---

## Documentation

| Link | Description |
|---|---|
| [Complete installation](docs/01_installation.md) | Detailed installation guide (release, dev, test) |
| [LedStorage and ScanBox](docs/02_storeLed_and_scanner.md) | ESP modules wiring and configuration |
| [Web interface](docs/03_frontend_usage.md) | Frontend user guide |
| [API](docs/04_api_usage.md) | REST API documentation |
| [OpenAPI / Swagger](https://vampi62.github.io/electrostore/openapi/) | Interactive API reference |

## Getting Started

After deployment, access the web interface at `http://<your-server>:8080`.

Default credentials:
- **User**: admin@localhost.local
- **Password**: Admin@1234

> Change the administrator password on first login.

<!-- SCREENSHOT: web interface login page -->
<!-- ![Login](docs/assets/screenshot_login.png) -->