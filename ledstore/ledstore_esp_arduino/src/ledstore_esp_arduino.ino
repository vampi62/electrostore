#if defined(ESP32)
  #include <WiFi.h> // Utilisez la bibliothèque WiFi pour ESP32
  #define LED_PIN 15
#elif defined(ESP8266)
  #include <ESP8266WiFi.h> // Utilisez la bibliothèque ESP8266WiFi pour ESP8266
  #define LED_PIN 2
#else
  #error "board not supported"
#endif

#include <ESPAsyncWebServer.h>
AsyncWebServer server(80);
#include <EEPROM.h>
#include <PubSubClient.h>
#include <Adafruit_NeoPixel.h>
#include <ArduinoJson.h>
#include <ArduinoOTA.h>

#define EEPROM_SIZE 512

// Définir les adresses de début dans l'EEPROM
#define WIFISSID_ADDRESS 0
#define WIFIPASSWORD_ADDRESS 51
#define ESPUSER_ADDRESS 102
#define ESPPASSWORD_ADDRESS 153
#define MQTTSERVER_ADDRESS 204
#define MQTTPORT_ADDRESS 255
#define MQTTNAME_ADDRESS 306
#define MQTTUSER_ADDRESS 357
#define MQTTPASSWORD_ADDRESS 408
#define MQTTTOPIC_ADDRESS 459

// Variables globales
String wifiSSID;
String wifiPassword;
String espUser;
String espPassword;
String mqttServer;
String mqttPort;
String mqttName;
String mqttUser;
String mqttPassword;
String mqttTopic;

WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);

int maxbuffer = 4096;

const char *ap_ssid = "ESP_Config";     // name of the WiFi network in AP mode
const char *ap_password = "ConfigPass"; // password of the WiFi network in AP mode

const char *version_ledstore = "1.1";

int ledCount = 256;
int nbrErreurMqttConnect = 0;
int nbrErreurWifiConnect = 0;
struct LEDInfo
{
  int red;
  int green;
  int blue;
  int module;
  int delayTime;
};
// the value in "leds" must be the same number than "ledCount"
LEDInfo leds[256];
Adafruit_NeoPixel strip(ledCount, LED_PIN, NEO_GRB + NEO_KHZ800);

float modSlow;
float modModerate;
float modQuick;
float modFast;
float inputSinus = 0;
bool isClientWIFI = false;
bool waitingOTA = false;
bool updateOTA = false;
String updateOTAError = "";
float otaPercentage = 0.0;
unsigned long startTimeOTA = 0;
unsigned long IntervalOTA = 120000; // 2 minutes
unsigned long connectionTimeoutWIFI = 10000; // 10 secondes
unsigned long startTimeWIFI;
unsigned long startTimeLed;
unsigned long delayTime;

#include "ledstore_eeprom.h"
#include "ledstore_wifimqtt.h"
#include "ledstore_pageMenu.h"
#include "ledstore_pageWifi.h"
#include "ledstore_pageUser.h"
#include "ledstore_pageMQTT.h"
#include "ledstore_pageOTA.h"

void callback(char *topic, byte *payload, unsigned int length)
{
  if (length == 0)
  {
    return;
  }
  Serial.print("Message reçu [");
  Serial.print(topic);
  Serial.println("] ");
  char json[length + 1];
  strncpy(json, (char *)payload, length);
  json[length] = '\0';
  DynamicJsonDocument doc(maxbuffer);
  DeserializationError error = deserializeJson(doc, json);
  if (error)
  {
    Serial.print("Erreur lors de la désérialisation JSON: ");
    Serial.println(error.c_str());
    return;
  }
  if (doc.containsKey("leds"))
  {
    JsonArray ledsArray = doc["leds"].as<JsonArray>();
    Serial.println(ledsArray.size() + " leds change");
    for (int i = 0; i < ledsArray.size(); i++)
    {
      int indextab = ledsArray[i]["index"];
      if (indextab >= ledCount)
      {
        continue;
      }
      leds[indextab + 1].red = ledsArray[i]["red"];
      leds[indextab + 1].green = ledsArray[i]["green"];
      leds[indextab + 1].blue = ledsArray[i]["blue"];
      leds[indextab + 1].module = ledsArray[i]["module"];
      leds[indextab + 1].delayTime = ledsArray[i]["delay"];
    }
    startTimeLed = millis();
  }
}

void setup()
{
  strip.begin();
  strip.setPixelColor(0, strip.Color(20, 20, 20));
  strip.show();
  Serial.begin(115200);
  EEPROM.begin(EEPROM_SIZE);
  wifiSSID = readStringFromEEPROM(WIFISSID_ADDRESS);
  wifiPassword = readStringFromEEPROM(WIFIPASSWORD_ADDRESS);
  espUser = readStringFromEEPROM(ESPUSER_ADDRESS);
  espPassword = readStringFromEEPROM(ESPPASSWORD_ADDRESS);
  mqttName = readStringFromEEPROM(MQTTNAME_ADDRESS);
  mqttUser = readStringFromEEPROM(MQTTUSER_ADDRESS);
  mqttPassword = readStringFromEEPROM(MQTTPASSWORD_ADDRESS);
  mqttTopic = readStringFromEEPROM(MQTTTOPIC_ADDRESS);
  mqttServer = readStringFromEEPROM(MQTTSERVER_ADDRESS);
  mqttPort = readStringFromEEPROM(MQTTPORT_ADDRESS);

  isClientWIFI = setupWiFi();
  if (isClientWIFI)
  {
    strip.setPixelColor(0, strip.Color(0, 20, 20));
    strip.show();
    mqttClient.setBufferSize(maxbuffer);
    mqttClient.setServer(mqttServer.c_str(), mqttPort.toInt());
    mqttClient.setCallback(callback);
    delay(500);
    reconnectMQTT();
  }
  else
  {
    strip.setPixelColor(0, strip.Color(20, 0, 0));
    strip.show();
  }

  ArduinoOTA.setHostname("LedStore");
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
  server.on("/mqtt", HTTP_GET, handleMenuMqtt);
  server.on("/savemqtt", HTTP_GET, handleSaveMqtt);
  server.on("/", HTTP_GET, handleRoot);
  server.on("/status", HTTP_GET, handleStatus);
  server.on("/ota", HTTP_GET, handleMenuOTA);
  server.on("/saveota", HTTP_GET, handleOTA);
/*   server.on("favicon.ico", HTTP_GET, [](AsyncWebServerRequest *request) {
    request->send_P(200, "image/x-icon", favicon, sizeof(favicon));
  }); */
  server.begin();
  strip.setPixelColor(0, strip.Color(0, 0, 0));
  strip.show();
}

void loop()
{
  if (isClientWIFI)
  {
    if (WiFi.status() != WL_CONNECTED)
    {
      Serial.println("wifi connection lost");
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
    }
    if (!mqttClient.connected())
    {
      reconnectMQTT();
    }
    else
    {
      mqttClient.loop();
    }
    inputSinus = inputSinus + 0.01;
    if (inputSinus >= 1080)
    {
      inputSinus = 0;
    }
    float modSlow = fabs(sin(inputSinus));
    float modModerate = fabs(sin(inputSinus / 0.5));
    float modQuick = fabs(sin(inputSinus / 0.25));
    float modFast = fabs(sin(inputSinus / 0.125));
    strip.clear();
    delayTime = millis();
    for (int i = 1; i < ledCount; i++)
    {
      if (leds[i].delayTime > 0)
      {
        switch (leds[i].module)
        {
          case 1:
            strip.setPixelColor(i, strip.Color(leds[i].red, leds[i].green, leds[i].blue));
            break;
          case 2:
            strip.setPixelColor(i, strip.Color(leds[i].red * modSlow, leds[i].green * modSlow, leds[i].blue * modSlow));
            break;
          case 3:
            strip.setPixelColor(i, strip.Color(leds[i].red * modModerate, leds[i].green * modModerate, leds[i].blue * modModerate));
            break;
          case 4:
            strip.setPixelColor(i, strip.Color(leds[i].red * modQuick, leds[i].green * modQuick, leds[i].blue * modQuick));
            break;
          case 5:
            strip.setPixelColor(i, strip.Color(leds[i].red * modFast, leds[i].green * modFast, leds[i].blue * modFast));
            break;
          default:
            strip.setPixelColor(i, strip.Color(leds[i].red, leds[i].green, leds[i].blue));
            break;
        }
        leds[i].delayTime = leds[i].delayTime - (delayTime - startTimeLed);
      }
    }
    startTimeLed = millis();
    strip.show();
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