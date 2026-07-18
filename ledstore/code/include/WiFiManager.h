#ifndef WIFI_MANAGER_H
#define WIFI_MANAGER_H

#if defined(ESP32)
#include <WiFi.h> // Utilisez la bibliothèque WiFi pour ESP32
#elif defined(ESP8266)
#include <ESP8266WiFi.h> // Utilisez la bibliothèque ESP8266WiFi pour ESP8266
#else
#error "board not supported"
#endif
#include <Arduino.h>

enum WifiConnectionMode {
    WIFI_CONN_AP,
    WIFI_CONN_CLIENT,
    WIFI_CONN_NONE
};

class WiFiManager {
private:
    String ssid;
    String password;
    int retryCount;
    WifiConnectionMode currentMode;
    unsigned long apStartTime;
public:
    explicit WiFiManager();

    bool begin();
    bool connectToWiFi(const String& ssid, const String& password);
    void startAPMode();
    void handleConnection();

    bool isConnected();
    WifiConnectionMode getCurrentMode();
    String getLocalIP();
    String getMACAddress();

    void saveCredentials(const String& ssid, const String& password);
    bool loadCredentials();

    String getSsid()    const { return ssid; }
    bool hasPassword()  const { return password.length() > 0; }
};

#endif