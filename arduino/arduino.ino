#include <WiFi.h>
#include <EEPROM.h>
#include <PubSubClient.h>
#include <WebServer.h>
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
#define LED_ADDRESS 256

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
WebServer server(80);

int maxbuffer = 4096;
#define LED_PIN 15 // Pin de données de la LED

int ledCount = 100;
struct LEDInfo {
  int red;
  int green;
  int blue;
  int module;
  int delayTime;
};
LEDInfo leds[100];
Adafruit_NeoPixel strip(ledCount, LED_PIN, NEO_GRB + NEO_KHZ800);

byte variation_led = 25;
byte min_led = 120;
float out;
float insinus = 0;
bool iswificlient = false;
unsigned long connectionTimeout = 30000; // 30 secondes
unsigned long startTime;
unsigned long delaytime;

void writeStringToEEPROM(int startAddress, const String& data) {
  int length = data.length();
  EEPROM.write(startAddress, length);
  for (int i = 0; i < length; i++) {
    EEPROM.write(startAddress + 1 + i, data[i]);
  }
  EEPROM.commit();
}

String readStringFromEEPROM(int startAddress) {
  int length = EEPROM.read(startAddress);
  String data = "";
  for (int i = 0; i < length; i++) {
    data += char(EEPROM.read(startAddress + 1 + i));
  }
  return data;
}

bool setupWiFi() {
  Serial.println();
  Serial.print("Connexion au réseau Wi-Fi: ");
  Serial.println(ssid);
  WiFi.begin(ssid.c_str(), password.c_str());
  startTime = millis();
  while (WiFi.status() != WL_CONNECTED && millis() - startTime < connectionTimeout) {
    delay(500);
    Serial.print(".");
  }
  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("");
    Serial.println("Wi-Fi connecté !");
    Serial.print("Adresse IP: ");
    Serial.println(WiFi.localIP());
    return true;
  } else {
    Serial.println("");
    Serial.println("Connexion au réseau Wi-Fi échouée.");
    ssid = "MonReseauTemp";
    password = "MotDePasseTemp";
    WiFi.softAP(ssid.c_str(), password.c_str());
    IPAddress myIP = WiFi.softAPIP();
    Serial.print("Adresse IP du réseau temporaire: ");
    Serial.println(myIP);
    return false;
  }
}

void callback(char* topic, byte* payload, unsigned int length) {
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
    for (int i = 0; i < ledsArray.size(); i++) {
      int indextab = ledsArray[i]["index"];
      Serial.println(indextab);
      leds[indextab+1].red = ledsArray[i]["red"];
      leds[indextab+1].green = ledsArray[i]["green"];
      leds[indextab+1].blue = ledsArray[i]["blue"];
      leds[indextab+1].module = ledsArray[i]["module"];
      leds[indextab+1].delayTime = ledsArray[i]["delay"];
    }
    startTime = millis();
  }
}

#include "testwifimenu.h"

bool reconnectMQTT() {
  // Connexion au serveur MQTT
  Serial.println();
  Serial.print("Connexion au serveur MQTT...");
  Serial.println(mqttname);
  startTime = millis();
  if (mqttClient.connect(mqttname.c_str(), mqttUser.c_str(), mqttPassword.c_str())) {
    Serial.println("connecté !");
    Serial.println(mqttTopic.c_str());
    mqttClient.subscribe(mqttTopic.c_str());
    strip.setPixelColor(0, strip.Color(0, 20, 0));
    strip.show();
  } else {
    Serial.print("échec, code d'erreur = ");
    Serial.print(mqttClient.state());
    strip.setPixelColor(0, strip.Color(20, 20, 0));
    strip.show();
  }
  delay(10);
  return mqttClient.connected();
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
  server.on("/menuwifi", handleMenuWifi);
  server.on("/savewifi", handleSaveWifi);
  server.on("/menumqtt", handleMenuMqtt);
  server.on("/savemqtt", handleSaveMqtt);
  server.on("/", handleRoot);
  server.begin();
  delay(4000);
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
  for (int i = 0; i < ledCount; i++) {
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
  server.handleClient();
}