#include <Arduino.h>
#include "config.h"
#include "WiFiManager.h"
#include "WebServer.h"
#include "StorageManager.h"
#include "MQTTManager.h"
#include "OTAManager.h"
#include "StripLedManager.h"
#include "Logger.h"

WiFiManager     wifiManager;
OTAManager      otaManager;
StripLedManager stripLedManager;
MQTTManager     mqttManager(&wifiManager);
WebServer       webServer(&wifiManager, &mqttManager, &otaManager);

void setup() {
    Serial.begin(115200);
    delay(1000);

    Logger::info("Démarrage ESP...");

    // Initialisation module StripLed ws2812b
    stripLedManager.begin();

    stripLedManager.setLed(0, 255, 0, 0, 1, 100); // LED 1 rouge

    // Initialisation du stockage
    if (!StorageManager::begin()) {
        Logger::error("Erreur initialisation LittleFS");
        // LED 1 rouge clignotante pour signaler l'erreur
        while (true) {
            stripLedManager.setLed(0, 255, 0, 0, 1, 100); // LED 1 rouge
            delay(250);
            stripLedManager.setLed(0, 0, 0, 0, 1, 100); // LED éteinte
            delay(250);
        }
    }

    stripLedManager.setLed(0, 0, 0, 255, 1, 100); // LED 1 bleue

    // Initialisation WiFi
    if (!wifiManager.begin()) {
        Logger::warning("Démarrage en mode AP");
        // LED 1 bleue rapide pour signaler le mode AP
        stripLedManager.setLed(0, 0, 0, 255, 3, 10000); // LED 1 bleue
    } else {
        Logger::info("Connexion WiFi établie");
        // LED 1 jaune pour signaler que la connexion WiFi est OK
        stripLedManager.setLed(0, 255, 255, 0, 1, 100); // LED 1 jaune
    }

    // Initialisation OTA
    otaManager.begin();

    // Initialisation MQTT
    mqttManager.setCallback([](const String& topic, const DynamicJsonDocument& payload) {
        if (payload.containsKey("leds"))
        {
            JsonArrayConst ledsArray = payload["leds"].as<JsonArrayConst>();
            Logger::info("Réception configuration leds via MQTT");
            Logger::info("Nombre de leds : " + String(ledsArray.size()));
            for (size_t i = 0; i < ledsArray.size(); i++)
            {
                int indextab = ledsArray[i]["index"];
                if (indextab >= LED_COUNT)
                {
                    continue;
                }
                if (HAS_LED_IN_BOX) {
                    // Si  LED dans le boîtier, on décale l'index pour ne pas écraser la LED 1
                    stripLedManager.leds[indextab + 1]->red = ledsArray[i]["red"];
                    stripLedManager.leds[indextab + 1]->green = ledsArray[i]["green"];
                    stripLedManager.leds[indextab + 1]->blue = ledsArray[i]["blue"];
                    stripLedManager.leds[indextab + 1]->module = ledsArray[i]["module"];
                    stripLedManager.leds[indextab + 1]->delayTime = ledsArray[i]["delay"];
                }
                else
                {
                    stripLedManager.leds[indextab]->red = ledsArray[i]["red"];
                    stripLedManager.leds[indextab]->green = ledsArray[i]["green"];
                    stripLedManager.leds[indextab]->blue = ledsArray[i]["blue"];
                    stripLedManager.leds[indextab]->module = ledsArray[i]["module"];
                    stripLedManager.leds[indextab]->delayTime = ledsArray[i]["delay"];
                }
            }
        }
    });
    if (!mqttManager.begin()) {
        Logger::warning("Échec initialisation MQTT");
        if (wifiManager.getCurrentMode() == WIFI_CONN_CLIENT) {
            // connecter en client wifi mais erreur de connexion MQTT, LED 1 jaune pour signaler l'erreur
            stripLedManager.setLed(0, 255, 255, 0, 3, 10000); // LED 1 jaune
        }
    } else {
        Logger::info("MQTT initialisé avec succès");
        // LED 1 verte pour signaler que la connexion MQTT est OK
        stripLedManager.setLed(0, 0, 255, 0, 1, 10000); // LED 1 verte
    }

    // Démarrage serveur web
    webServer = WebServer(&wifiManager, &mqttManager, &otaManager);
    webServer.begin();

    Logger::info("Setup terminé");
}

void loop() {
    wifiManager.handleConnection();
    mqttManager.handleConnection();
    otaManager.handle();
    static unsigned long lastShow = 0;
    if (millis() - lastShow >= 20) {  // ~50 fps
        lastShow = millis();
        stripLedManager.show();
    }
}