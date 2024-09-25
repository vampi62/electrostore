#include "esp_camera.h"
#include <WiFi.h>
#include <WebServer.h>
#include "esp_timer.h"
#include "img_converters.h"
#include "Arduino.h"
#include "fb_gfx.h"
#include "soc/soc.h" //disable brownout problems
#include "soc/rtc_cntl_reg.h"  //disable brownout problems
#include <Adafruit_NeoPixel.h>
#include <EEPROM.h>

#define EEPROM_SIZE 512

// Définir les adresses de début dans l'EEPROM
#define SSID_ADDRESS 0
#define PASSWORD_ADDRESS 32
#define CAMUSER_ADDRESS 160
#define CAMPASSWORD_ADDRESS 192

// Variables globales
String ssid;
String password;
String camUser;
String camPassword;

WebServer server(80);
WiFiClient wifiClient;

const char *ap_ssid = "ESP_Config"; // Nom du réseau WiFi en mode AP (point d'accès)
const char *ap_password = "ConfigPass"; // Mot de passe du réseau WiFi en mode AP

bool iswificlient = false;
unsigned long connectionTimeout = 10000; // 10 secondes
unsigned long startTime;
unsigned long delaytime;

int nbrErreurWifiConnect = 0;
#define LED_PIN 15
Adafruit_NeoPixel strip(64, LED_PIN, NEO_GRB + NEO_KHZ800);

#include "prgeeprom.h"
#include "prgwifi.h"
#include "prgpagehttp.h"



#define PART_BOUNDARY "123456789000000000000987654321"
#define CAMERA_MODEL_AI_THINKER
#define PWDN_GPIO_NUM     32
#define RESET_GPIO_NUM    -1
#define XCLK_GPIO_NUM      0
#define SIOD_GPIO_NUM     26
#define SIOC_GPIO_NUM     27

#define Y9_GPIO_NUM       35
#define Y8_GPIO_NUM       34
#define Y7_GPIO_NUM       39
#define Y6_GPIO_NUM       36
#define Y5_GPIO_NUM       21
#define Y4_GPIO_NUM       19
#define Y3_GPIO_NUM       18
#define Y2_GPIO_NUM        5
#define VSYNC_GPIO_NUM    25
#define HREF_GPIO_NUM     23
#define PCLK_GPIO_NUM     22

void setup() {
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
  
  if(psramFound()){
    config.frame_size = FRAMESIZE_UXGA;
    config.jpeg_quality = 10;
    config.fb_count = 2;
  } else {
    config.frame_size = FRAMESIZE_SVGA;
    config.jpeg_quality = 12;
    config.fb_count = 1;
  }
  // Camera init
  esp_err_t err = esp_camera_init(&config);
  if (err != ESP_OK) {
    Serial.printf("Camera init failed with error 0x%x", err);
    return;
  }
  strip.begin();
  strip.setPixelColor(0, strip.Color(20, 20, 20));
  strip.show();
  WRITE_PERI_REG(RTC_CNTL_BROWN_OUT_REG, 0); //disable brownout detector
  Serial.begin(115200);
  Serial.setDebugOutput(false);
  EEPROM.begin(EEPROM_SIZE);
  ssid = readStringFromEEPROM(SSID_ADDRESS);
  password = readStringFromEEPROM(PASSWORD_ADDRESS);
  camUser = readStringFromEEPROM(CAMUSER_ADDRESS);
  camPassword = readStringFromEEPROM(CAMPASSWORD_ADDRESS);

  iswificlient = setupWiFi();
  if (iswificlient) {
    strip.setPixelColor(0, strip.Color(0, 20, 20));
    strip.show();
  } else {
    strip.setPixelColor(0, strip.Color(20, 0, 0));
    strip.show();
  }
  server.on("/menuwifi", HTTP_GET, handleMenuWifi);
  server.on("/savewifi", HTTP_GET, handleSaveWifi);
  server.on("/menucam", HTTP_GET, handleMenuCam);
  server.on("/savecam", HTTP_GET, handleSaveCam);
  server.on("/light", HTTP_GET, handleLight);
  server.on("/", HTTP_GET, handleRoot);
  server.on("/stream", HTTP_GET, stream_handler);
  server.begin();
  strip.setPixelColor(0, strip.Color(0, 0, 0));
  for (int i = 1; i < 64; i++) {
    strip.setPixelColor(i, strip.Color(50, 50, 50));
  }
  strip.show();
}

void loop() {
  if (iswificlient) {
    if (WiFi.status() != WL_CONNECTED) {
      Serial.println("Connexion au réseau Wi-Fi perdue.");
      iswificlient = setupWiFi();
      if (iswificlient) {
        strip.setPixelColor(0, strip.Color(0, 20, 20));
        strip.show();
      } else {
        strip.setPixelColor(0, strip.Color(20, 0, 0));
        strip.show();
      }
    }
  }
  server.handleClient();
}