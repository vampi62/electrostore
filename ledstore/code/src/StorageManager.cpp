#include "StorageManager.h"
#include "Logger.h"
#include "config.h"

bool StorageManager::begin() {
#if defined(ESP32)
    if (!LittleFS.begin(true)) {
#elif defined(ESP8266)
    if (!LittleFS.begin()) {
#endif
        Logger::error("Erreur montage LittleFS");
        return false;
    }
    Logger::info("LittleFS monté avec succès");
    return true;
}

// ---------------------------------------------------------------------------
// Fonctions génériques privées
// ---------------------------------------------------------------------------

bool StorageManager::saveJson(const String& filePath, const JsonDocument& doc) {
    File file = LittleFS.open(filePath, "w");
    if (!file) {
        Logger::error("Erreur ouverture fichier: " + filePath);
        return false;
    }
    if (serializeJson(doc, file) == 0) {
        Logger::error("Erreur écriture JSON: " + filePath);
        file.close();
        return false;
    }
    file.close();
    return true;
}

bool StorageManager::loadJson(const String& filePath, JsonDocument& doc) {
    if (!LittleFS.exists(filePath)) {
        Logger::warning("Fichier inexistant: " + filePath);
        return false;
    }
    File file = LittleFS.open(filePath, "r");
    if (!file) {
        Logger::error("Erreur lecture fichier: " + filePath);
        return false;
    }
    DeserializationError error = deserializeJson(doc, file);
    file.close();
    if (error) {
        Logger::error("Erreur parsing JSON (" + filePath + "): " + String(error.c_str()));
        return false;
    }
    return true;
}

// ---------------------------------------------------------------------------
// Config WiFi
// ---------------------------------------------------------------------------

bool StorageManager::saveConfig(const String& ssid, const String& password) {
    StaticJsonDocument<256> doc;
    doc["ssid"]     = ssid;
    doc["password"] = password;
    bool ok = saveJson(CONFIG_FILE, doc);
    if (ok) Logger::info("Configuration sauvegardée");
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
    if (ok) Logger::info("Identifiants auth sauvegardés");
    return ok;
}

bool StorageManager::loadAuth(String& user, String& password) {
    StaticJsonDocument<256> doc;
    if (!loadJson(AUTH_FILE, doc)) return false;
    user     = doc["user"].as<String>();
    password = doc["password"].as<String>();
    return !user.isEmpty();
}

// ---------------------------------------------------------------------------
// Config MQTT
// ---------------------------------------------------------------------------

bool StorageManager::saveMQTTConfig(const String& server, int port, const String& user, const String& password, const String& topic) {
    StaticJsonDocument<512> doc;
    doc["server"]   = server;
    doc["port"]     = port;
    doc["user"]     = user;
    doc["password"] = password;
    doc["topic"]    = topic;
    bool ok = saveJson(MQTT_CONFIG_FILE, doc);
    if (ok) Logger::info("Configuration MQTT sauvegardée");
    return ok;
}

bool StorageManager::loadMQTTConfig(String& server, int& port, String& user, String& password, String& topic) {
    StaticJsonDocument<512> doc;
    if (!loadJson(MQTT_CONFIG_FILE, doc)) return false;
    server   = doc["server"].as<String>();
    port     = doc["port"].as<int>();
    user     = doc["user"].as<String>();
    password = doc["password"].as<String>();
    topic    = doc["topic"].as<String>();
    return !server.isEmpty();
}
