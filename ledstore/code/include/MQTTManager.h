#ifndef MQTT_MANAGER_H
#define MQTT_MANAGER_H

#include <Arduino.h>
#if defined(ESP32)
  #include <WiFi.h> // Utilisez la bibliothèque WiFi pour ESP32
#elif defined(ESP8266)
  #include <ESP8266WiFi.h> // Utilisez la bibliothèque ESP8266WiFi pour ESP8266
#else
  #error "board not supported"
#endif
#include <PubSubClient.h>
#include "WiFiManager.h"
#include <functional>
#include "config.h"
#include <ArduinoJson.h>

class MQTTManager {
private:
    WiFiClient   wifiClient;
    WiFiManager* wifiManager;
    PubSubClient mqttClient;

    static MQTTManager* _instance;

    String mqttServer;
    int    mqttPort;
    String mqttUser;
    String mqttPassword;
    String mqttTopic;     // "<topic>"
    String topicBase;    // "electrostore/<topic>"
    String statusTopic;  // "electrostore/<topic>/status"
    String sessionName;  // "<prefix><MAC>"
    unsigned long   _lastReconnectAttempt;

    using MessageCallback = std::function<void(const String& topic, const JsonDocument& payload)>;
    MessageCallback messageCallback;
    static void staticCallback(char* topic, uint8_t* payload, unsigned int length);
public:
    explicit MQTTManager(WiFiManager* wm);

    // Initialise la connexion. Retourne true si la première connexion réussit.
    bool begin();
    bool connectToMQTT(const String& server, int port,
                       const String& user, const String& password,
                       const String& topic, const String& clientPrefix);

    void handleConnection();

    bool publish(const String& subtopic, const String& payload, bool retained = false);

    bool isConnected();
    void setCallback(MessageCallback cb);

    void saveCredentials(const String& server, int port,
                         const String& user, const String& password,
                         const String& topic);
    bool loadCredentials();

    String getServer()    const { return mqttServer; }
    int    getPort()      const { return mqttPort; }
    String getUser()      const { return mqttUser; }
    bool   hasPassword()  const { return mqttPassword.length() > 0; }
    String getRawTopic()  const {
        String prefix = String(MQTT_BASE_TOPIC) + "/";
        return topicBase.startsWith(prefix) ? topicBase.substring(prefix.length()) : topicBase;
    }
};

#endif
