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

// OTA configuration
#define OTA_HOSTNAME        "ESP32_Jardin"
#define OTA_DEFAULT_PASSWORD "electostore"
#define OTA_PORT            8100
#define OTA_WINDOW_MS       300000  // OTA window: 5 minutes after startup

// StripLed configuration
#define LED_COUNT 30
#define HAS_LED_IN_BOX true
#if defined(ESP32)
#define LED_PIN 15
#else
#error "board not supported"
#endif

// Web server configuration
#define WEB_SERVER_PORT 80

#define VERSION "1.2"

#endif