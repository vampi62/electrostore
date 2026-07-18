#include "WiFiManager.h"
#include "StorageManager.h"
#include "Logger.h"
#include "config.h"

WiFiManager::WiFiManager() : retryCount(0), currentMode(WIFI_CONN_NONE), apStartTime(0) {}

bool WiFiManager::begin() {
    // Tentative de chargement des credentials
    if (loadCredentials()) {
        Logger::info("Credentials trouvés, tentative de connexion...");
        
        if (connectToWiFi(ssid, password)) {
            currentMode = WIFI_CONN_CLIENT;
            Logger::info("Connecté au WiFi: " + ssid);
            Logger::info("IP: " + getLocalIP());
            return true;
        }
    }
    
    // Échec ou pas de credentials -> Mode AP
    startAPMode();
    return false;
}

bool WiFiManager::connectToWiFi(const String& ssid, const String& password) {
    WiFi.mode(WIFI_STA);
    WiFi.begin(ssid.c_str(), password.c_str());
    
    Logger::info("Connexion à " + ssid + "...");
    
    unsigned long startAttempt = millis();
    
    while (WiFi.status() != WL_CONNECTED && 
           millis() - startAttempt < WIFI_CONNECT_TIMEOUT) {
        delay(500);
        Serial.print(".");
    }
    Serial.println();
    
    if (WiFi.status() == WL_CONNECTED) {
        retryCount = 0;
        return true;
    }
    
    retryCount++;
    Logger::warning("Échec connexion (tentative " + String(retryCount) + ")");
    
    if (retryCount >= WIFI_MAX_RETRIES) {
        Logger::error("Nombre max de tentatives atteint");
        retryCount = 0;
        return false;
    }
    
    return false;
}

void WiFiManager::startAPMode() {
#if defined(ESP32)
    WiFi.mode((wifi_mode_t)WIFI_AP);
#elif defined(ESP8266)
    WiFi.mode(WIFI_AP);
#endif
    WiFi.softAP(AP_SSID, AP_PASSWORD);
    
    currentMode = WIFI_CONN_AP;
    apStartTime = millis();
    
    Logger::info("Mode AP démarré");
    Logger::info("SSID: " + String(AP_SSID));
    Logger::info("IP: " + WiFi.softAPIP().toString());
}

void WiFiManager::handleConnection() {
    // En mode Client, vérifier la connexion
    if (currentMode == WIFI_CONN_CLIENT && WiFi.status() != WL_CONNECTED) {
        Logger::warning("Connexion perdue, reconnexion...");
        if (!connectToWiFi(ssid, password)) {
            startAPMode();
        }
    }
}

bool WiFiManager::isConnected() {
    return currentMode == WIFI_CONN_CLIENT && WiFi.status() == WL_CONNECTED;
}

WifiConnectionMode WiFiManager::getCurrentMode() {
    return currentMode;
}

String WiFiManager::getLocalIP() {
    if (currentMode == WIFI_CONN_CLIENT) {
        return WiFi.localIP().toString();
    } else if (currentMode == WIFI_CONN_AP) {
        return WiFi.softAPIP().toString();
    }
    return "0.0.0.0";
}

String WiFiManager::getMACAddress() {
    return WiFi.macAddress();
}

void WiFiManager::saveCredentials(const String& newSsid, const String& newPassword) {
    ssid = newSsid;
    password = newPassword;
    StorageManager::saveConfig(ssid, password);
}

bool WiFiManager::loadCredentials() {
    return StorageManager::loadConfig(ssid, password);
}