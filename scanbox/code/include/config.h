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
#define OTA_HOSTNAME        "ESP32_ScanBox"
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

// Camera configuration (AI-Thinker ESP32-CAM pinout)
#define CAMERA_MODEL_AI_THINKER
#define CAM_PIN_PWDN     32
#define CAM_PIN_RESET    -1
#define CAM_PIN_XCLK      0
#define CAM_PIN_SIOD     26
#define CAM_PIN_SIOC     27
#define CAM_PIN_D7       35
#define CAM_PIN_D6       34
#define CAM_PIN_D5       39
#define CAM_PIN_D4       36
#define CAM_PIN_D3       21
#define CAM_PIN_D2       19
#define CAM_PIN_D1       18
#define CAM_PIN_D0        5
#define CAM_PIN_VSYNC    25
#define CAM_PIN_HREF     23
#define CAM_PIN_PCLK     22

#define CAM_XCLK_FREQ_HZ         20000000
#define CAM_FRAME_SIZE_PSRAM     FRAMESIZE_QXGA
#define CAM_JPEG_QUALITY_PSRAM   10
#define CAM_FRAME_SIZE_NOPSRAM   FRAMESIZE_SVGA
#define CAM_JPEG_QUALITY_NOPSRAM 12

// Web server configuration
#define WEB_SERVER_PORT 80

#define VERSION "1.2"

#endif