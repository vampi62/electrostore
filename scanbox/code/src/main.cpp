#include <Arduino.h>
#include "config.h"
#include "WiFiManager.h"
#include "WebServer.h"
#include "StorageManager.h"
#include "OTAManager.h"
#include "StripLedManager.h"
#include "CameraManager.h"
#include "Logger.h"

WiFiManager     wifiManager;
OTAManager      otaManager;
StripLedManager stripLedManager;
CameraManager   cameraManager;
WebServer       webServer(&wifiManager, &otaManager, &cameraManager, &stripLedManager);

void setup() {
    Serial.begin(115200);
    delay(1000);

    Logger::info("Starting ESP...");

    // Initialize camera (does not depend on WiFi/storage)
    if (!cameraManager.begin()) {
        Logger::error("Camera initialization failed");
    }

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

    // Start web server
    webServer.begin();

    Logger::info("Setup complete");
}

void loop() {
    wifiManager.handleConnection();
    otaManager.handle();
    static unsigned long lastShow = 0;
    if (millis() - lastShow >= 20) {  // ~50 fps
        lastShow = millis();
        stripLedManager.show();
    }
}
