#ifndef WEB_SERVER_H
#define WEB_SERVER_H

#include <ESPAsyncWebServer.h>
#include "WiFiManager.h"
#include "OTAManager.h"
#include "CameraManager.h"
#include "StripLedManager.h"

#include "StorageManager.h"

class WebServer {
private:
    AsyncWebServer* server;
    WiFiManager* wifiManager;
    OTAManager* otaManager;
    CameraManager* cameraManager;
    StripLedManager* stripLedManager;
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

    void handleCamPage(AsyncWebServerRequest *request);
    void handleSaveCam(AsyncWebServerRequest *request);
    void handleCapture(AsyncWebServerRequest *request);
    void handleStream(AsyncWebServerRequest *request);

    void handleSaveLight(AsyncWebServerRequest *request);

    void handleSendStyle(AsyncWebServerRequest *request);
    void handleSendJS(AsyncWebServerRequest *request);

    void handleNotFound(AsyncWebServerRequest *request);
public:
    explicit WebServer(WiFiManager* wm, OTAManager* om, CameraManager* cm, StripLedManager* sm);
    ~WebServer();
    WebServer(const WebServer&) = delete;
    WebServer& operator=(const WebServer&) = delete;

    void begin();
    void stop();

    void getCredentials(String& user, String& password) const {
        user = espUser;
        password = espPassword;
    }
};

#endif