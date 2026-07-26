#include "StorageManager.h"
#include "Logger.h"
#include "config.h"

bool StorageManager::begin() {
#if defined(ESP32)
    if (!LittleFS.begin(true)) {
#elif defined(ESP8266)
    if (!LittleFS.begin()) {
#endif
        Logger::error("LittleFS mount error");
        return false;
    }
    Logger::info("LittleFS mounted successfully");
    return true;
}

// ---------------------------------------------------------------------------
// Private generic functions
// ---------------------------------------------------------------------------

bool StorageManager::saveJson(const String& filePath, const JsonDocument& doc) {
    File file = LittleFS.open(filePath, "w");
    if (!file) {
        Logger::error("Error opening file: " + filePath);
        return false;
    }
    if (serializeJson(doc, file) == 0) {
        Logger::error("Error writing JSON: " + filePath);
        file.close();
        return false;
    }
    file.close();
    return true;
}

bool StorageManager::loadJson(const String& filePath, JsonDocument& doc) {
    if (!LittleFS.exists(filePath)) {
        Logger::warning("File does not exist: " + filePath);
        return false;
    }
    File file = LittleFS.open(filePath, "r");
    if (!file) {
        Logger::error("Error reading file: " + filePath);
        return false;
    }
    DeserializationError error = deserializeJson(doc, file);
    file.close();
    if (error) {
        Logger::error("Error parsing JSON (" + filePath + "): " + String(error.c_str()));
        return false;
    }
    return true;
}

// ---------------------------------------------------------------------------
// WiFi Config
// ---------------------------------------------------------------------------

bool StorageManager::saveConfig(const String& ssid, const String& password) {
    StaticJsonDocument<256> doc;
    doc["ssid"]     = ssid;
    doc["password"] = password;
    bool ok = saveJson(CONFIG_FILE, doc);
    if (ok) Logger::info("Configuration saved");
    return ok;
}

bool StorageManager::loadConfig(String& ssid, String& password) {
    StaticJsonDocument<256> doc;
    if (!loadJson(CONFIG_FILE, doc)) return false;
    ssid     = doc["ssid"].as<String>();
    password = doc["password"].as<String>();
    return !ssid.isEmpty();
}

// ---------------------------------------------------------------------------
// Auth
// ---------------------------------------------------------------------------

bool StorageManager::saveAuth(const String& user, const String& password) {
    StaticJsonDocument<256> doc;
    doc["user"]     = user;
    doc["password"] = password;
    bool ok = saveJson(AUTH_FILE, doc);
    if (ok) Logger::info("Auth credentials saved");
    return ok;
}

bool StorageManager::loadAuth(String& user, String& password) {
    StaticJsonDocument<256> doc;
    if (!loadJson(AUTH_FILE, doc)) return false;
    user     = doc["user"].as<String>();
    password = doc["password"].as<String>();
    return !user.isEmpty();
}
