#ifndef STORAGE_MANAGER_H
#define STORAGE_MANAGER_H

#include <LittleFS.h>
#include <ArduinoJson.h>

class StorageManager {
private:
    static bool saveJson(const String& filePath, const JsonDocument& doc);
    static bool loadJson(const String& filePath, JsonDocument& doc);
public:
    static bool begin();
    static bool saveConfig(const String& ssid, const String& password);
    static bool loadConfig(String& ssid, String& password);
    static bool saveAuth(const String& user, const String& password);
    static bool loadAuth(String& user, String& password);
};

#endif