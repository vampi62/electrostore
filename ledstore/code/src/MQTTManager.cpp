#include "MQTTManager.h"
#include "StorageManager.h"
#include "Logger.h"

MQTTManager* MQTTManager::_instance = nullptr;

MQTTManager::MQTTManager(WiFiManager* wm) : wifiManager(wm)
, mqttClient(wifiClient), mqttPort(1883), _lastReconnectAttempt(0) {}

bool MQTTManager::begin() {
    _instance = this;
    // Attempt to load credentials
    if (loadCredentials()) {
        Logger::info("MQTT credentials found, attempting to connect...");
        mqttClient.setServer(mqttServer.c_str(), mqttPort);
        mqttClient.setCallback(staticCallback);
        if (wifiManager->isConnected() && wifiManager->getCurrentMode() == WIFI_CONN_CLIENT) {
            if (connectToMQTT(mqttServer, mqttPort, mqttUser, mqttPassword, mqttTopic, sessionName)) {
                return true;
            }
        }
    }
    return false;
}

bool MQTTManager::connectToMQTT(const String& server, int port,
                       const String& user, const String& password,
                       const String& topic, const String& clientPrefix) {
    Logger::info("MQTT connection " + server + ":" + String(port) + "...");
    String mac = wifiManager->getMACAddress();
    mac.replace(":", "");
    sessionName = clientPrefix + mac;
    topicBase   = String(MQTT_BASE_TOPIC) + "/" + topic;
    statusTopic = topicBase + "/status";

    bool connected = mqttClient.connect(
        sessionName.c_str(),
        mqttUser.c_str(),
        mqttPassword.c_str(),
        statusTopic.c_str(),   // LWT topic
        1,                     // LWT QoS
        true,                  // LWT retained
        "offline"              // LWT message
    );

    if (connected) {
        mqttClient.subscribe(topicBase.c_str());
        publish("status", "online", true);
        Logger::info("MQTT connected, subscribed to: " + topicBase);
        return true;
    }

    Logger::error("MQTT connection failed, rc=" + String(mqttClient.state()));
    return false;
}

void MQTTManager::handleConnection() {
    if (!mqttClient.connected() && wifiManager->isConnected() && wifiManager->getCurrentMode() == WIFI_CONN_CLIENT) {
        unsigned long now = millis();
        if (now - _lastReconnectAttempt >= MQTT_RECONNECT_INTERVAL) {
            _lastReconnectAttempt = now;
            Logger::info("Attempting MQTT reconnection...");
            connectToMQTT(mqttServer, mqttPort, mqttUser, mqttPassword, mqttTopic, sessionName);
        }
    }
    if (mqttClient.connected()) {
        mqttClient.loop();
    }
}

bool MQTTManager::publish(const String& subtopic, const String& payload, bool retained) {
    if (!mqttClient.connected()) {
        Logger::warning("Attempted MQTT publish while the client is not connected.");
        return false;
    }
    String fullTopic = topicBase + "/" + subtopic;
    bool ok = mqttClient.publish(fullTopic.c_str(), payload.c_str(), retained);
    if (!ok) Logger::error("MQTT publish failed on: " + fullTopic);
    return ok;
}

bool MQTTManager::isConnected() {
    return mqttClient.connected();
}

void MQTTManager::setCallback(MessageCallback cb) {
    messageCallback = cb;
}

// Static bridge -> instance: converts raw PubSubClient types to String
void MQTTManager::staticCallback(char* topic, uint8_t* payload, unsigned int length) {
    if (!_instance) return;
    Logger::debug("Message received on topic: " + String(topic));
    char json[length + 1];
    strncpy(json, (const char*)payload, length);
    json[length] = '\0';
    DynamicJsonDocument doc(MQTT_BUFFER_SIZE);
    DeserializationError error = deserializeJson(doc, json);
    if (error) {
        Logger::error("JSON deserialization error: " + String(error.c_str()));
        return;
    }
    if (_instance->messageCallback) {
        _instance->messageCallback(String(topic), doc);
    }
}

void MQTTManager::saveCredentials(const String& newServer, int newPort,
                                  const String& newUser, const String& newPassword,
                                  const String& newTopic) {
    mqttServer   = newServer;
    mqttPort     = newPort;
    mqttUser     = newUser;
    mqttPassword = newPassword;
    mqttTopic    = newTopic;
    topicBase   = String(MQTT_BASE_TOPIC) + "/" + mqttTopic;
    mqttClient.setServer(mqttServer.c_str(), mqttPort);
    mqttClient.setCallback(staticCallback);
    if (mqttClient.connected()) {
        mqttClient.disconnect();
    }
    StorageManager::saveMQTTConfig(mqttServer, mqttPort, mqttUser, mqttPassword, mqttTopic);
}

bool MQTTManager::loadCredentials() {
    return StorageManager::loadMQTTConfig(mqttServer, mqttPort, mqttUser, mqttPassword, mqttTopic);
}