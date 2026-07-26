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

    Logger::info("Starting ESP...");

    // Initialize StripLed ws2812b module
    stripLedManager.begin();

    stripLedManager.setLed(0, 255, 0, 0, 1, 100); // LED 1 red

    // Initialize storage
    if (!StorageManager::begin()) {
        Logger::error("LittleFS initialization error");
        // LED 1 blinking red to signal the error
        while (true) {
            stripLedManager.setLed(0, 255, 0, 0, 1, 100); // LED 1 red
            delay(250);
            stripLedManager.setLed(0, 0, 0, 0, 1, 100); // LED off
            delay(250);
        }
    }

    stripLedManager.setLed(0, 0, 0, 255, 1, 100); // LED 1 blue

    // Initialize WiFi
    if (!wifiManager.begin()) {
        Logger::warning("Starting in AP mode");
        // LED 1 fast blue to signal AP mode
        stripLedManager.setLed(0, 0, 0, 255, 3, 10000); // LED 1 blue
    } else {
        Logger::info("WiFi connection established");
        // LED 1 yellow to signal that the WiFi connection is OK
        stripLedManager.setLed(0, 255, 255, 0, 1, 100); // LED 1 yellow
    }

    // Initialize OTA
    otaManager.begin();

    // Initialize MQTT
    mqttManager.setCallback([](const String& topic, const DynamicJsonDocument& payload) {
        if (payload.containsKey("leds"))
        {
            JsonArrayConst ledsArray = payload["leds"].as<JsonArrayConst>();
            Logger::info("Received leds configuration via MQTT");
            Logger::info("Number of leds: " + String(ledsArray.size()));
            for (size_t i = 0; i < ledsArray.size(); i++)
            {
                int indextab = ledsArray[i]["index"];
                if (indextab >= LED_COUNT)
                {
                    continue;
                }
                if (HAS_LED_IN_BOX) {
                    // If a LED is in the box, shift the index to avoid overwriting LED 1
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
        Logger::warning("MQTT initialization failed");
        if (wifiManager.getCurrentMode() == WIFI_CONN_CLIENT) {
            // Connected as WiFi client but MQTT connection error, LED 1 yellow to signal the error
            stripLedManager.setLed(0, 255, 255, 0, 3, 10000); // LED 1 yellow
        }
    } else {
        Logger::info("MQTT initialized successfully");
        // LED 1 green to signal that the MQTT connection is OK
        stripLedManager.setLed(0, 0, 255, 0, 1, 10000); // LED 1 green
    }

    // Start web server
    webServer = WebServer(&wifiManager, &mqttManager, &otaManager);
    webServer.begin();

    Logger::info("Setup complete");
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