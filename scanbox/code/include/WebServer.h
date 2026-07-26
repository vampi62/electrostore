#ifndef WEB_SERVER_H
#define WEB_SERVER_H

#include <ESPAsyncWebServer.h>
#include "WiFiManager.h"
#include "MQTTManager.h"
#include "OTAManager.h"

#include "StorageManager.h"

class WebServer {
private:
    AsyncWebServer* server;
    WiFiManager* wifiManager;
    MQTTManager* mqttManager;
    OTAManager* otaManager;
    String espUser;
    String espPassword;

    bool authenticate(AsyncWebServerRequest *request);
    void setupRoutes();

    void handleRoot(AsyncWebServerRequest *request);
    void handleStatus(AsyncWebServerRequest *request);

    void handleWiFiPage(AsyncWebServerRequest *request);
    void handleSaveWiFi(AsyncWebServerRequest *request);

    void handleAuthPage(AsyncWebServerRequest *request);
    void handleSaveAuth(AsyncWebServerRequest *request);

    void handleOTAPage(AsyncWebServerRequest *request);
    void handleSaveOTA(AsyncWebServerRequest *request);

    void handleMQTTPage(AsyncWebServerRequest *request);
    void handleSaveMQTT(AsyncWebServerRequest *request);

    void handleSendStyle(AsyncWebServerRequest *request);
    void handleSendJS(AsyncWebServerRequest *request);

    void handleNotFound(AsyncWebServerRequest *request);
public:
    explicit WebServer(WiFiManager* wm, MQTTManager* mm, OTAManager* om);
    ~WebServer();

    void begin();
    void stop();

    void getCredentials(String& user, String& password) const {
        user = espUser;
        password = espPassword;
    }
};

#endif