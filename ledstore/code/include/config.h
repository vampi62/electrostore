#ifndef CONFIG_H
#define CONFIG_H

// Configuration WiFi AP
#define AP_SSID "ESP_Config"
#define AP_PASSWORD "ConfigPass"

// Configuration WiFi Client
#define WIFI_CONNECT_TIMEOUT 10000  // 10 secondes
#define WIFI_MAX_RETRIES 3

// Configuration Stockage
#define CONFIG_FILE      "/config.json"
#define AUTH_FILE        "/auth.json"
#define MQTT_CONFIG_FILE "/mqtt_config.json"

// Configuration OTA
#define OTA_HOSTNAME        "ESP32_Jardin"
#define OTA_DEFAULT_PASSWORD "electostore"
#define OTA_PORT            8100
#define OTA_WINDOW_MS       300000  // Fenêtre OTA : 5 minutes après le démarrage

// Configuration MQTT
#define MQTT_RECONNECT_INTERVAL 5000          // ms entre tentatives de reconnexion
#define MQTT_BASE_TOPIC         "electrostore"
#define MQTT_CLIENT_PREFIX      "ESP32"
#define MQTT_BUFFER_SIZE          4096  // Taille du buffer MQTT (doit être >= à la taille maximale des messages attendus)

// Configuration StripLed
#define LED_COUNT 30
#define HAS_LED_IN_BOX true
#if defined(ESP32)
#define LED_PIN 15
#elif defined(ESP8266)
#define LED_PIN 2
#else
#error "board not supported"
#endif

// Configuration Serveur Web
#define WEB_SERVER_PORT 80

#define VERSION "1.2"

#endif