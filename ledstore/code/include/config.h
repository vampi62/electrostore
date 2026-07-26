#ifndef CONFIG_H
#define CONFIG_H

// WiFi AP configuration
#define AP_SSID "ESP_Config"
#define AP_PASSWORD "ConfigPass"

// WiFi Client configuration
#define WIFI_CONNECT_TIMEOUT 10000  // 10 seconds
#define WIFI_MAX_RETRIES 3

// Storage configuration
#define CONFIG_FILE      "/config.json"
#define AUTH_FILE        "/auth.json"
#define MQTT_CONFIG_FILE "/mqtt_config.json"

// OTA configuration
#define OTA_HOSTNAME        "ESP32_StoreLed"
#define OTA_DEFAULT_PASSWORD "electostore"
#define OTA_PORT            8100
#define OTA_WINDOW_MS       300000  // OTA window: 5 minutes after startup

// MQTT configuration
#define MQTT_RECONNECT_INTERVAL 5000          // ms between reconnection attempts
#define MQTT_BASE_TOPIC         "electrostore"
#define MQTT_CLIENT_PREFIX      "ESP32"
#define MQTT_BUFFER_SIZE          4096  // MQTT buffer size (must be >= the maximum expected message size)

// StripLed configuration
#define LED_COUNT 30
#define HAS_LED_IN_BOX true
#if defined(ESP32)
#define LED_PIN 15
#elif defined(ESP8266)
#define LED_PIN 2
#else
#error "board not supported"
#endif

// Web server configuration
#define WEB_SERVER_PORT 80

#define VERSION "1.2"

#endif