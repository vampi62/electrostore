#if defined(ESP32)
  #include <WiFi.h> // Utilisez la bibliothèque WiFi pour ESP32
  #include <WebServer.h>
  WebServer server(80);
  #define LED_PIN 15
#elif defined(ESP8266)
  #include <ESP8266WiFi.h> // Utilisez la bibliothèque ESP8266WiFi pour ESP8266
  #include <ESP8266WebServer.h>
  ESP8266WebServer server(80);
  #define LED_PIN 2
#else
  #error "Type de carte non pris en charge !"
#endif
#include <EEPROM.h>
#include <PubSubClient.h>
#include <Adafruit_NeoPixel.h>
#include <ArduinoJson.h>

#define EEPROM_SIZE 512

// Définir les adresses de début dans l'EEPROM
#define SSID_ADDRESS 0
#define PASSWORD_ADDRESS 32
#define MQTTSERVER_ADDRESS 64
#define MQTTPORT_ADDRESS 96
#define MQTTNAME_ADDRESS 128
#define MQTTUSER_ADDRESS 160
#define MQTTPASSWORD_ADDRESS 192
#define MQTTTOPIC_ADDRESS 224

// Variables globales
String ssid;
String password;
String mqttServer;
String mqttPort;
String mqttname;
String mqttUser;
String mqttPassword;
String mqttTopic;

WiFiClient wifiClient;
PubSubClient mqttClient(wifiClient);

int maxbuffer = 4096;

const char *ap_ssid = "ESP_Config"; // Nom du réseau WiFi en mode AP (point d'accès)
const char *ap_password = "ConfigPass"; // Mot de passe du réseau WiFi en mode AP

int ledCount = 256;
int nbrErreurMqttConnect = 0;
int nbrErreurWifiConnect = 0;
struct LEDInfo {
  int red;
  int green;
  int blue;
  int module;
  int delayTime;
};
// la valeur dans leds doit être identique a "ledCount"
LEDInfo leds[256];
Adafruit_NeoPixel strip(ledCount, LED_PIN, NEO_GRB + NEO_KHZ800);

float outlent;
float outmoyen;
float outrapide;
float insinus = 0;
bool iswificlient = false;
unsigned long connectionTimeout = 10000; // 10 secondes
unsigned long startTime;
unsigned long delaytime;

#include "prgeeprom.h"
#include "prgwifimqtt.h"
#include "prgpagehttp.h"

void callback(char* topic, byte* payload, unsigned int length) {
  if (length == 0) {
    return;
  }
  Serial.print("Message reçu [");
  Serial.print(topic);
  Serial.println("] ");
  char json[length + 1];
  strncpy(json, (char*)payload, length);
  json[length] = '\0';
  DynamicJsonDocument doc(maxbuffer);
  DeserializationError error = deserializeJson(doc, json);
  if (error) {
    Serial.print("Erreur lors de la désérialisation JSON: ");
    Serial.println(error.c_str());
    return;
  }
  if (doc.containsKey("leds")) {
    JsonArray ledsArray = doc["leds"].as<JsonArray>();
    Serial.println(ledsArray.size() + " leds change");
    for (int i = 0; i < ledsArray.size(); i++) {
      int indextab = ledsArray[i]["index"];
      if (indextab >= ledCount) {
        continue;
      }
      leds[indextab+1].red = ledsArray[i]["red"];
      leds[indextab+1].green = ledsArray[i]["green"];
      leds[indextab+1].blue = ledsArray[i]["blue"];
      leds[indextab+1].module = ledsArray[i]["module"];
      leds[indextab+1].delayTime = ledsArray[i]["delay"];
    }
    startTime = millis();
  }
}

void setup() {
  strip.begin();
  strip.setPixelColor(0, strip.Color(20, 20, 20));
  strip.show();
  Serial.begin(115200);
  EEPROM.begin(EEPROM_SIZE);
  ssid = readStringFromEEPROM(SSID_ADDRESS);
  password = readStringFromEEPROM(PASSWORD_ADDRESS);
  mqttname = readStringFromEEPROM(MQTTNAME_ADDRESS);
  mqttUser = readStringFromEEPROM(MQTTUSER_ADDRESS);
  mqttPassword = readStringFromEEPROM(MQTTPASSWORD_ADDRESS);
  mqttTopic = readStringFromEEPROM(MQTTTOPIC_ADDRESS);
  mqttServer = readStringFromEEPROM(MQTTSERVER_ADDRESS);
  mqttPort = readStringFromEEPROM(MQTTPORT_ADDRESS);

  iswificlient = setupWiFi();
  if (iswificlient) {
    strip.setPixelColor(0, strip.Color(0, 20, 20));
    strip.show();
    mqttClient.setBufferSize(maxbuffer);
    mqttClient.setServer(mqttServer.c_str(), mqttPort.toInt());
    mqttClient.setCallback(callback);
    delay(500);
    reconnectMQTT();
  } else {
    strip.setPixelColor(0, strip.Color(20, 0, 0));
    strip.show();
  }
  server.on("/menuwifi", HTTP_GET, handleMenuWifi);
  server.on("/savewifi", HTTP_GET, handleSaveWifi);
  server.on("/menumqtt", HTTP_GET, handleMenuMqtt);
  server.on("/savemqtt", HTTP_GET, handleSaveMqtt);
  server.on("/", HTTP_GET, handleRoot);
  server.begin();
  strip.setPixelColor(0, strip.Color(0, 0, 0));
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
    if (!mqttClient.connected()) {
      reconnectMQTT();
    } else {
      mqttClient.loop();
    }
    insinus = insinus + 0.01;
    if (insinus >= 1080) {
      insinus = 0;
    }
    float outlent = fabs(sin(insinus / 3));
    float outmoyen = fabs(sin(insinus / 2));
    float outrapide = fabs(sin(insinus / 1));
    strip.clear();
    delaytime = millis();
    for (int i = 1; i < ledCount; i++) {
      if (leds[i].delayTime > 0) {
        if (leds[i].module == 1) {
          strip.setPixelColor(i, strip.Color(leds[i].red, leds[i].green, leds[i].blue));
        } else if (leds[i].module == 2) {
          strip.setPixelColor(i, strip.Color(leds[i].red * outlent, leds[i].green * outlent, leds[i].blue * outlent));
        } else if (leds[i].module == 3) {
          strip.setPixelColor(i, strip.Color(leds[i].red * outmoyen, leds[i].green * outmoyen, leds[i].blue * outmoyen));
        } else if (leds[i].module == 4) {
          strip.setPixelColor(i, strip.Color(leds[i].red * outrapide, leds[i].green * outrapide, leds[i].blue * outrapide));
        } else {
          strip.setPixelColor(i, strip.Color(leds[i].red, leds[i].green, leds[i].blue));
        }
        leds[i].delayTime = leds[i].delayTime - (delaytime - startTime);
      }
    }
    startTime = millis();
    strip.show();
  }
  server.handleClient();
}