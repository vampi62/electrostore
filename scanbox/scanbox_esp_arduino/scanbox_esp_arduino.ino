#include "esp_camera.h"
#include <WiFi.h>
#include <ESPAsyncWebServer.h>
#include "esp_timer.h"
#include "img_converters.h"
#include "Arduino.h"
#include "fb_gfx.h"
#include "soc/soc.h"          //disable brownout problems
#include "soc/rtc_cntl_reg.h" //disable brownout problems
#include <Adafruit_NeoPixel.h>
#include <EEPROM.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>

#define EEPROM_SIZE 512

// Définir les adresses de début dans l'EEPROM
#define WIFISSID_ADDRESS 0
#define WIFIPASSWORD_ADDRESS 51
#define ESPUSER_ADDRESS 102
#define ESPPASSWORD_ADDRESS 153

// Variables globales
String wifiSSID;
String wifiPassword;
String espUser;
String espPassword;

AsyncWebServer server(80);
WiFiClient wifiClient;

const char *ap_ssid = "ESP_Config";     // Nom du réseau WiFi en mode AP (point d'accès)
const char *ap_password = "ConfigPass"; // Mot de passe du réseau WiFi en mode AP

const char *version_scanbox = "1.1";
String camResolution;


bool isClientWIFI = false;
bool waitingOTA = false;
bool updateOTA = false;
String updateOTAError = "";
float otaPercentage = 0.0;
unsigned long startTimeOTA = 0;
unsigned long IntervalOTA = 120000; // 2 minutes
unsigned long connectionTimeoutWIFI = 10000; // 10 secondes
unsigned long startTimeWIFI;

unsigned int ringLightPower = 0;

int nbrErreurWifiConnect = 0;
#define LED_PIN 15
Adafruit_NeoPixel strip(64, LED_PIN, NEO_GRB + NEO_KHZ800);

#include "scanbox_eeprom.h"
#include "scanbox_wifi.h"
#include "scanbox_pageMenu.h"
#include "scanbox_pageWifi.h"
#include "scanbox_pageUser.h"
#include "scanbox_pageCam.h"
#include "scanbox_pageCamVideoFlux.h"
#include "scanbox_pageOTA.h"

#define CAMERA_MODEL_AI_THINKER
#define PWDN_GPIO_NUM 32
#define RESET_GPIO_NUM -1
#define XCLK_GPIO_NUM 0
#define SIOD_GPIO_NUM 26
#define SIOC_GPIO_NUM 27

#define Y9_GPIO_NUM 35
#define Y8_GPIO_NUM 34
#define Y7_GPIO_NUM 39
#define Y6_GPIO_NUM 36
#define Y5_GPIO_NUM 21
#define Y4_GPIO_NUM 19
#define Y3_GPIO_NUM 18
#define Y2_GPIO_NUM 5
#define VSYNC_GPIO_NUM 25
#define HREF_GPIO_NUM 23
#define PCLK_GPIO_NUM 22

void setup()
{
  Serial.begin(115200);
  Serial.setDebugOutput(false);
  camera_config_t config;
  config.ledc_channel = LEDC_CHANNEL_0;
  config.ledc_timer = LEDC_TIMER_0;
  config.pin_d0 = Y2_GPIO_NUM;
  config.pin_d1 = Y3_GPIO_NUM;
  config.pin_d2 = Y4_GPIO_NUM;
  config.pin_d3 = Y5_GPIO_NUM;
  config.pin_d4 = Y6_GPIO_NUM;
  config.pin_d5 = Y7_GPIO_NUM;
  config.pin_d6 = Y8_GPIO_NUM;
  config.pin_d7 = Y9_GPIO_NUM;
  config.pin_xclk = XCLK_GPIO_NUM;
  config.pin_pclk = PCLK_GPIO_NUM;
  config.pin_vsync = VSYNC_GPIO_NUM;
  config.pin_href = HREF_GPIO_NUM;
  config.pin_sscb_sda = SIOD_GPIO_NUM;
  config.pin_sscb_scl = SIOC_GPIO_NUM;
  config.pin_pwdn = PWDN_GPIO_NUM;
  config.pin_reset = RESET_GPIO_NUM;
  config.xclk_freq_hz = 20000000;
  config.pixel_format = PIXFORMAT_JPEG;
  if (psramFound())
  {
    config.frame_size = FRAMESIZE_QXGA;
    camResolution = "FRAMESIZE_QXGA";
    Serial.printf("résolution : FRAMESIZE_QXGA, ");
    config.jpeg_quality = 10;
    config.fb_count = 2;
  }
  else
  {
    config.frame_size = FRAMESIZE_SVGA;
    camResolution = "FRAMESIZE_SVGA";
    Serial.printf("résolution : FRAMESIZE_SVGA, ");
    config.jpeg_quality = 12;
    config.fb_count = 1;
  }
  // Camera init
  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK)
  {
    Serial.printf("Camera init failed with error 0x%x", err);
    return;
  }
  sensor_t *s = esp_camera_sensor_get();
  if (s)
  {
    switch (s->id.PID)
    {
    case 0x26: // OV2640 (PID 38 en hexadécimal)
      Serial.printf("Caméra found : OV2640");
      break;
    case 0x5640: // OV5640 (PID 22080 en décimal, 0x5640 en hexadécimal)
      Serial.printf("Caméra found : OV5640");
      break;
    default:
      Serial.printf("Caméra found : %X", s->id.PID);
      break;
    }
  }
  else
  {
    Serial.printf("Error getting sensor information");
  }
  strip.begin();
  strip.setPixelColor(0, strip.Color(20, 20, 20));
  strip.show();
  WRITE_PERI_REG(RTC_CNTL_BROWN_OUT_REG, 0); // disable brownout detector
  EEPROM.begin(EEPROM_SIZE);
  wifiSSID = readStringFromEEPROM(WIFISSID_ADDRESS);
  wifiPassword = readStringFromEEPROM(WIFIPASSWORD_ADDRESS);
  espUser = readStringFromEEPROM(ESPUSER_ADDRESS);
  espPassword = readStringFromEEPROM(ESPPASSWORD_ADDRESS);

  isClientWIFI = setupWiFi();
  if (isClientWIFI)
  {
    strip.setPixelColor(0, strip.Color(0, 20, 20));
    strip.show();
  }
  else
  {
    strip.setPixelColor(0, strip.Color(20, 0, 0));
    strip.show();
  }

  ArduinoOTA.setHostname("ScanBox");
  ArduinoOTA.setPort(8100);
  if (espPassword.length() > 0)
  {
    ArduinoOTA.setPassword(espPassword.c_str());
  }
  else
  {
    ArduinoOTA.setPassword("0");
  }
  ArduinoOTA.onStart([]() {
    Serial.println("Start");
    updateOTA = true;
  });
  ArduinoOTA.onEnd([]() {
    Serial.println("\nEnd");
    updateOTA = false;
  });
  ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
    otaPercentage = (progress / (total / 100));
    Serial.printf("Progress: %u%%\r", otaPercentage);
  });
  ArduinoOTA.onError([](ota_error_t error) {
    Serial.printf("Error[%u]: ", error);
    updateOTAError = String(error);
    updateOTA = false;
    if (error == OTA_AUTH_ERROR) Serial.println("Auth Failed");
    else if (error == OTA_BEGIN_ERROR) Serial.println("Begin Failed");
    else if (error == OTA_CONNECT_ERROR) Serial.println("Connect Failed");
    else if (error == OTA_RECEIVE_ERROR) Serial.println("Receive Failed");
    else if (error == OTA_END_ERROR) Serial.println("End Failed");
  });
  ArduinoOTA.begin();

  server.on("/wifi", HTTP_GET, handleMenuWifi);
  server.on("/savewifi", HTTP_GET, handleSaveWifi);
  server.on("/user", HTTP_GET, handleMenuUser);
  server.on("/saveuser", HTTP_GET, handleSaveUser);
  server.on("/cam", HTTP_GET, handleMenuCam);
  //server.on("/savecam", HTTP_GET, handleSaveCam);
  server.on("/light", HTTP_GET, handleLight);
  server.on("/", HTTP_GET, handleRoot);
  server.on("/stream", HTTP_GET, handleStream);
  server.on("/capture", HTTP_GET, handleCapture);
  server.on("/status", HTTP_GET, handleStatus);
  server.on("/ota", HTTP_GET, handleMenuOTA);
  server.on("/saveota", HTTP_GET, handleOTA);
/*   server.on("favicon.ico", HTTP_GET, [](AsyncWebServerRequest *request) {
    request->send_P(200, "image/x-icon", favicon, sizeof(favicon));
  }); */
  server.begin();
  strip.setPixelColor(0, strip.Color(0, 0, 0));
  for (int i = 1; i < 64; i++)
  {
    strip.setPixelColor(i, strip.Color(50, 50, 50));
  }
  ringLightPower = 50;
  strip.show();
}

void loop()
{
  if (isClientWIFI)
  {
    if (WiFi.status() != WL_CONNECTED)
    {
      Serial.println("Wifi connection lost");
      isClientWIFI = setupWiFi();
      if (isClientWIFI)
      {
        strip.setPixelColor(0, strip.Color(0, 20, 20));
        strip.show();
        delay(1000);
        strip.setPixelColor(0, strip.Color(0, 0, 0));
        strip.show();
      }
      else
      {
        strip.setPixelColor(0, strip.Color(20, 0, 0));
        strip.show();
      }
    }
  }
  if (waitingOTA)
  {
    if ((millis() - startTimeOTA > IntervalOTA) && (!updateOTA))
    {
      waitingOTA = false;
    }
    ArduinoOTA.handle();
  }
}