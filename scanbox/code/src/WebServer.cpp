#include "WebServer.h"
#include "Logger.h"
#include "config.h"
#include "StorageManager.h"

namespace {
    const char* CAM_STREAM_CONTENT_TYPE = "multipart/x-mixed-replace;boundary=123456789000000000000987654321";
    const char* CAM_STREAM_BOUNDARY     = "\r\n--123456789000000000000987654321\r\n";
    const char* CAM_STREAM_PART         = "Content-Type: image/jpeg\r\nContent-Length: %u\r\n\r\n";
    const char* CAM_JPG_CONTENT_TYPE    = "image/jpeg";

    // Serves a single JPEG frame, releasing it back to the camera driver once fully sent
    class CameraFrameResponse : public AsyncAbstractResponse {
    private:
        CameraManager* cam;
        camera_fb_t*   fb;
        size_t         _index;
    public:
        CameraFrameResponse(CameraManager* camera, camera_fb_t* frame) : cam(camera), fb(frame), _index(0) {
            _code          = 200;
            _contentLength = frame->len;
            _contentType   = CAM_JPG_CONTENT_TYPE;
        }
        ~CameraFrameResponse() {
            if (fb) cam->release(fb);
        }
        bool _sourceValid() const override { return fb != nullptr; }
        size_t _fillBuffer(uint8_t *buf, size_t maxLen) override {
            size_t remaining = fb->len - _index;
            size_t toCopy = maxLen < remaining ? maxLen : remaining;
            memcpy(buf, fb->buf + _index, toCopy);
            _index += toCopy;
            if (_index == fb->len) {
                cam->release(fb);
                fb = nullptr;
            }
            return toCopy;
        }
    };

    // Serves an MJPEG stream, pulling a new frame from the camera each time the buffer drains
    class CameraStreamResponse : public AsyncAbstractResponse {
    private:
        CameraManager* cam;
        camera_fb_t*   fb;
        size_t         _index;
    public:
        explicit CameraStreamResponse(CameraManager* camera) : cam(camera), fb(nullptr), _index(0) {
            _code              = 200;
            _contentLength     = 0;
            _contentType       = CAM_STREAM_CONTENT_TYPE;
            _sendContentLength = false;
            _chunked           = true;
        }
        ~CameraStreamResponse() {
            if (fb) cam->release(fb);
        }
        bool _sourceValid() const override { return true; }
        size_t _fillBuffer(uint8_t *buf, size_t maxLen) override {
            if (!fb) {
                if (maxLen < strlen(CAM_STREAM_BOUNDARY) + 64) {
                    return RESPONSE_TRY_AGAIN;
                }
                fb = cam->capture();
                if (!fb) {
                    return 0;
                }
                _index = 0;

                size_t pos  = strlen(CAM_STREAM_BOUNDARY);
                memcpy(buf, CAM_STREAM_BOUNDARY, pos);
                pos += sprintf((char*)(buf + pos), CAM_STREAM_PART, fb->len);

                size_t avail = maxLen - pos;
                size_t chunk = avail < fb->len ? avail : fb->len;
                memcpy(buf + pos, fb->buf, chunk);
                _index += chunk;

                if (_index == fb->len) {
                    cam->release(fb);
                    fb = nullptr;
                }
                return pos + chunk;
            }

            size_t remaining = fb->len - _index;
            size_t toCopy = maxLen < remaining ? maxLen : remaining;
            memcpy(buf, fb->buf + _index, toCopy);
            _index += toCopy;
            if (_index == fb->len) {
                cam->release(fb);
                fb = nullptr;
            }
            return toCopy;
        }
    };
}

WebServer::WebServer(WiFiManager* wm, OTAManager* om, CameraManager* cm, StripLedManager* sm) : wifiManager(wm), otaManager(om), cameraManager(cm), stripLedManager(sm) {
    server = new AsyncWebServer(WEB_SERVER_PORT);
}

WebServer::~WebServer() {
    server->end();
}

void WebServer::begin() {
    StorageManager::loadAuth(espUser, espPassword);
    if (wifiManager->isConnected() && wifiManager->getCurrentMode() == WIFI_CONN_CLIENT && espPassword.length() > 0) {
        otaManager->setPassword(espPassword);
    }
    setupRoutes();
    server->begin();
}

bool WebServer::authenticate(AsyncWebServerRequest *request) {
    // In AP mode, authentication is disabled to allow reset
    if (!wifiManager) return true;
    if (wifiManager->getCurrentMode() == WIFI_CONN_AP) {
        return true;
    }
    if (espUser.length() == 0 || espPassword.length() == 0) {
        return true;
    }
    if (!request->authenticate(espUser.c_str(), espPassword.c_str())) {
        request->requestAuthentication();
        return false;
    }
    return true;
}

void WebServer::setupRoutes() {
    // Menu page
    server->on("/", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleRoot(request);
    });

    // Status JSON
    server->on("/status", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleStatus(request);
    });

    // WiFi page
    server->on("/wifi", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleWiFiPage(request);
    });

    // Save WiFi
    server->on("/wifi", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveWiFi(request);
    });

    // Credentials management page
    server->on("/auth", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleAuthPage(request);
    });

    // Save credentials
    server->on("/auth", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveAuth(request);
    });

    // OTA page
    server->on("/ota", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleOTAPage(request);
    });

    // Save OTA
    server->on("/ota", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveOTA(request);
    });

    // Camera settings page
    server->on("/cam", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleCamPage(request);
    });

    // Save camera settings
    server->on("/cam", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveCam(request);
    });

    // Single JPEG snapshot
    server->on("/capture", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleCapture(request);
    });

    // MJPEG live stream
    server->on("/stream", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleStream(request);
    });

    // Enable/disable the 30-LED ring light
    server->on("/light", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveLight(request);
    });

    // Static files (CSS, JS)
    server->on("/style.css", HTTP_GET, [this](AsyncWebServerRequest *request) {
        handleSendStyle(request);
    });

    // Static files (CSS, JS)
    server->on("/common.js", HTTP_GET, [this](AsyncWebServerRequest *request) {
        handleSendJS(request);
    });

    // 404
    server->onNotFound([this](AsyncWebServerRequest *request) {
        handleNotFound(request);
    });
}

void WebServer::handleRoot(AsyncWebServerRequest *request) {
    String html = R"(
<!DOCTYPE html>
<html lang='en'>
    <head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>Menu</title>
    <style>
        body { font-family: Arial, sans-serif; background-color: #f4f4f9; color: #333; margin: 0; padding: 20px; }
        h1 { text-align: center; color: #4CAF50; }
        ul { list-style-type: none; padding: 0; max-width: 400px; margin: 20px auto; }
        li { margin: 10px 0; }
        .menu a { display: block; text-align: center; padding: 10px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; }
        .menu a:hover { background-color: #45a049; }
        .info { max-width:400px; margin:10px auto; font-size:12px; color:#888; text-align:center}
        .info a { text-align: center; margin: 3px; text-decoration: none; color: #4CAF50; }
        .info a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <h1>Menu</h1>
    <ul class='menu'>
        <li><a href='/wifi'>WiFi Settings</a></li>
        <li><a href='/auth'>User Settings</a></li>
        <li><a href='/ota'>OTA Settings</a></li>
        <li><a href='/cam'>Camera Settings</a></li>
    </ul>
    <div class='info'>
        <b>Version:</b> ")";
    html += VERSION;
    html += R"("<br>
        <b>credit:</b> Created by <b>vampi62</b>. Visit <a href='https://github.com/vampi62/electrostore'>Github Project</a> for more information.</b>
    </div>
</body>
</html>
)";

    request->send(200, "text/html", html);
}

void WebServer::handleStatus(AsyncWebServerRequest *request) {
    StaticJsonDocument<500> doc;
    doc["uptime"] = millis() / 1000;
    doc["espModel"] = ESP.getChipModel();
    doc["OTAWait"] = otaManager->isWindowOpen();
    doc["OTAUploading"] = otaManager->isUpdating();
    doc["OTAError"] = otaManager->getLastError();
    doc["OTATime"] = OTA_WINDOW_MS / 1000;
    doc["OTARemainingTime"] = otaManager->getRemainingTime();
    doc["OTAPercentage"] = otaManager->getProgress();
    doc["versionLedStore"] = VERSION;
    doc["wifiSignalStrength"] = String(WiFi.RSSI());
    doc["WifiConnectionMode"] = (WiFi.getMode() == WIFI_MODE_AP) ? "AP" : "STA";
    doc["wifiSSID"] = WiFi.SSID();
    doc["wifiIP"] = WiFi.localIP().toString();
    doc["wifiStatus"] = (WiFi.status() == WL_CONNECTED) ? "Connected" : "Disconnected";
    doc["wifiMAC"] = WiFi.macAddress();
    doc["ringLightOn"] = stripLedManager->isRingLightOn();
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(200, "application/json", jsonResponse);
}

void WebServer::handleWiFiPage(AsyncWebServerRequest *request) {
    String curSsid = wifiManager->getSsid();
    bool hasPwd = wifiManager->hasPassword();
    String html =
        "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>WiFi Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>WiFi Settings</h1><div id='notification'></div>"
        "<div class='card'>"
        "<div class='fg'><label>SSID</label>"
        "<input type='text' id='ssid' value='" + curSsid + "' maxlength='50'></div>"
        "<div class='fg'><input type='checkbox' id='usePwd' onchange='tglPwd()' " +
        String(hasPwd ? "checked" : "") +
        "> Password required</div>"
        "<div class='fg'><label>Password</label>"
        "<input type='password' id='pwd' maxlength='50' " +
        String(hasPwd ? "placeholder='&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;'" : "disabled") +
        "></div>"
        "<button onclick='save()'>Save &amp; Restart</button></div>"
        "<a class='back' href='/'>&#8592; Back</a>"
        "<div class='card'>"
        "<b>IP:</b> " + WiFi.localIP().toString() +
        " &nbsp; <b>MAC:</b> " + WiFi.macAddress() +
        "<br><b>Current SSID:</b> " + WiFi.SSID() +
        " &nbsp; <b>Signal:</b> <span id='rssi'>" + String(WiFi.RSSI()) + " dBm</span>"
        "<br><b>Mode:</b> " + String(WiFi.getMode() == WIFI_MODE_AP ? "AP" : "STA") +
        "</div>"
        "<script src='/common.js'></script><script>"
        "function tglPwd(){var f=document.getElementById('pwd');"
        "f.disabled=!document.getElementById('usePwd').checked;f.value='';}"
        "function save(){var s=document.getElementById('ssid').value;"
        "var p=document.getElementById('usePwd').checked?document.getElementById('pwd').value:'';"
        "apiPost('/wifi',{ssid:s,password:p});}"
        "setInterval(function(){fetch('/status').then(function(r){return r.json()})"
        ".then(function(d){document.getElementById('rssi').innerText=d.wifiSignalStrength+' dBm';});},5000);"
        "</script></body></html>";
    request->send(200, "text/html", html);
}

void WebServer::handleSaveWiFi(AsyncWebServerRequest *request) {
    if (!request->hasParam("ssid", true)) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Missing ssid parameter\"}");
        return;
    }
    String ssid     = request->getParam("ssid", true)->value();
    String password = request->hasParam("password", true) ? request->getParam("password", true)->value() : "";
    wifiManager->saveCredentials(ssid, password);
    request->send(200, "application/json", "{\"status\":\"ok\",\"msg\":\"Credentials saved. Restarting...\"}");
    delay(1000);
    ESP.restart();
}

void WebServer::handleAuthPage(AsyncWebServerRequest *request) {
    bool hasCred = (espPassword.length() > 0);
    bool isAP    = (wifiManager->getCurrentMode() == WIFI_CONN_AP);
    String html =
        "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>Access Management</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>Access Management</h1><div id='notification'></div>";
    if (isAP)
        html += "<div class='alert alert-warn'>&#9888; AP mode: authentication disabled.</div>";
    else if (!hasCred)
        html += "<div class='alert alert-info'>&#9432; No credentials configured. Free access.</div>";
    html +=
        "<div class='card'>"
        "<div class='fg'><label>Username</label>"
        "<input type='text' id='usr' maxlength='50'></div>"
        "<div class='fg'><label>New password</label>"
        "<input type='password' id='np' maxlength='50'></div>"
        "<div class='fg'><label>Confirm password</label>"
        "<input type='password' id='cp' maxlength='50'></div>";
    if (hasCred && !isAP)
        html += "<div class='fg'><label>Old password</label>"
                "<input type='password' id='op' maxlength='50'></div>";
    html +=
        "<button onclick='save()'>Save</button></div>"
        "<a class='back' href='/'>&#8592; Back</a>"
        "<script src='/common.js'></script><script>"
        "function save(){"
        "var d={user:document.getElementById('usr').value,"
        "newpass:document.getElementById('np').value,"
        "confirmpass:document.getElementById('cp').value};";
    if (hasCred && !isAP)
        html += "var op=document.getElementById('op');if(op)d.oldpass=op.value;";
    html += "apiPost('/auth',d);}"
            "</script></body></html>";
    request->send(200, "text/html", html);
}

void WebServer::handleSaveAuth(AsyncWebServerRequest *request) {
    if (!request->hasParam("user", true) ||
        !request->hasParam("newpass", true) ||
        !request->hasParam("confirmpass", true)) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Missing parameters\"}");
        return;
    }
    String newUser  = request->getParam("user",        true)->value();
    String newPass  = request->getParam("newpass",     true)->value();
    String confPass = request->getParam("confirmpass", true)->value();
    if (newUser.indexOf(':') >= 0) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Invalid username (no ':')\"}");
        return;
    }
    if (newPass != confPass) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Passwords do not match\"}");
        return;
    }
    bool isAP = (wifiManager->getCurrentMode() == WIFI_CONN_AP);
    if (espPassword.length() > 0 && !isAP) {
        if (!request->hasParam("oldpass", true)) {
            request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Old password required\"}");
            return;
        }
        if (request->getParam("oldpass", true)->value() != espPassword) {
            request->send(401, "application/json", "{\"status\":\"error\",\"msg\":\"Incorrect old password\"}");
            return;
        }
    }
    espUser     = newUser;
    espPassword = newPass;
    StorageManager::saveAuth(espUser, espPassword);
    otaManager->setPassword(espPassword);
    request->send(200, "application/json", "{\"status\":\"ok\",\"msg\":\"Credentials updated successfully\"}");
}


void WebServer::handleOTAPage(AsyncWebServerRequest *request) {
    bool winOpen = otaManager->isWindowOpen();
    bool updating = otaManager->isUpdating();
    uint8_t prog = otaManager->getProgress();
    String lastErr = otaManager->getLastError();
    unsigned long rem = otaManager->getRemainingTime();
    String badge = winOpen
        ? (updating ? "<span class='badge badge-warn'>In progress</span>" : "<span class='badge badge-ok'>Open</span>")
        : "<span class='badge badge-err'>Closed</span>";
    String html =
        "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>OTA Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>OTA Settings</h1><div id='notification'></div>"
        "<div class='card'><h2>Status</h2>"
        "<p><b>Window:</b> " + badge + "</p>"
        "<p><b>Remaining:</b> <span id='otaRem'>" + String(rem / 1000) + "</span> s</p>"
        "<div class='progress'><div class='progress-bar' id='otaPb' style='width:" + String(prog) + "%'></div></div>";
    if (lastErr.length() > 0)
        html += "<p style='color:#e53935;font-size:13px'>" + lastErr + "</p>";
    html +=
        "<button class='btn-blue' onclick='openWin()'>Open OTA window (" +
        String(OTA_WINDOW_MS / 1000) + " s)</button></div>"
        "<a class='back' href='/'>&#8592; Back</a>"
        "<script src='/common.js'></script><script>"
        "function openWin(){apiPost('/ota',{},function(){setTimeout(refresh,500)});}"
        "function refresh(){fetch('/status').then(function(r){return r.json()}).then(function(d){"
        "var e=document.getElementById('otaRem');if(e)e.innerText=d.OTARemainingTime||0;"
        "var pb=document.getElementById('otaPb');if(pb)pb.style.width=(d.OTAPercentage||0)+'%';});}"
        "setInterval(refresh,2000);"
        "</script></body></html>";
    request->send(200, "text/html", html);
}

void WebServer::handleSaveOTA(AsyncWebServerRequest *request) {
    otaManager->openWindow(OTA_WINDOW_MS);
    StaticJsonDocument<96> doc;
    doc["status"] = "ok";
    doc["msg"] = "OTA window opened for " + String(OTA_WINDOW_MS / 1000) + "s";
    String out; serializeJson(doc, out);
    request->send(200, "application/json", out);
}

void WebServer::handleCamPage(AsyncWebServerRequest *request) {
    bool ready = cameraManager->isInitialized();
    String html =
        "<!DOCTYPE html><html lang='en'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>Camera Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>Camera Settings</h1><div id='notification'></div>";
    if (!ready) {
        html += "<div class='alert alert-warn'>&#9888; Camera not initialized.</div>";
    } else {
        html +=
            "<img src='/stream' style='display:block;max-width:100%;margin:15px auto;border-radius:8px'>"
            "<div class='card'>"
            "<p><b>Sensor:</b> " + cameraManager->getSensorName() + "</p>"
            "<p><b>Resolution:</b> " + cameraManager->getFrameSizeName() + "</p>"
            "<div class='fg'><label>Frame size</label>"
            "<select id='fs'>"
            "<option value='VGA'>VGA (640x480)</option>"
            "<option value='SVGA'>SVGA (800x600)</option>"
            "<option value='XGA'>XGA (1024x768)</option>"
            "<option value='HD'>HD (1280x720)</option>"
            "<option value='UXGA'>UXGA (1600x1200)</option>"
            "<option value='QXGA'>QXGA (2048x1536)</option>"
            "</select></div>"
            "<div class='fg'><label>JPEG quality (4-63, lower = better)</label>"
            "<input type='number' id='q' min='4' max='63' value='12'></div>"
            "<div class='fg'><label>Brightness (-2..2)</label>"
            "<input type='number' id='b' min='-2' max='2' value='0'></div>"
            "<div class='fg'><label>Contrast (-2..2)</label>"
            "<input type='number' id='c' min='-2' max='2' value='0'></div>"
            "<div class='fg'><label>Saturation (-2..2)</label>"
            "<input type='number' id='s' min='-2' max='2' value='0'></div>"
            "<div class='fg'><input type='checkbox' id='hm'> Mirror horizontally</div>"
            "<div class='fg'><input type='checkbox' id='vf'> Flip vertically</div>"
            "<button onclick='save()'>Apply</button>"
            "<button class='btn-blue' onclick=\"window.open('/capture','_blank')\">Capture snapshot</button>"
            "</div>";
    }
    html +=
        "<div class='card'>"
        "<button id='lightBtn' class='btn-blue' onclick='toggleLight()'>Ring light</button>"
        "</div>"
        "<a class='back' href='/'>&#8592; Back</a>"
        "<script src='/common.js'></script><script>"
        "var lightOn=false;"
        "function updateLightBtn(){"
        "var b=document.getElementById('lightBtn');"
        "if(b)b.innerText=lightOn?'Turn ring light OFF':'Turn ring light ON';"
        "}"
        "function toggleLight(){"
        "var next=!lightOn;"
        "apiPost('/light',{on:next?1:0},function(){lightOn=next;updateLightBtn();});"
        "}"
        "fetch('/status').then(function(r){return r.json()}).then(function(d){"
        "lightOn=!!d.ringLightOn;updateLightBtn();"
        "});"
        "function save(){apiPost('/cam',{"
        "framesize:document.getElementById('fs').value,"
        "quality:document.getElementById('q').value,"
        "brightness:document.getElementById('b').value,"
        "contrast:document.getElementById('c').value,"
        "saturation:document.getElementById('s').value,"
        "hmirror:document.getElementById('hm').checked?1:0,"
        "vflip:document.getElementById('vf').checked?1:0"
        "},function(){setTimeout(function(){location.reload();},500);});}"
        "</script></body></html>";
    request->send(200, "text/html", html);
}

void WebServer::handleSaveCam(AsyncWebServerRequest *request) {
    if (!cameraManager->isInitialized()) {
        request->send(503, "application/json", "{\"status\":\"error\",\"msg\":\"Camera not initialized\"}");
        return;
    }
    if (request->hasParam("framesize", true)) {
        String fsName = request->getParam("framesize", true)->value();
        framesize_t fs = FRAMESIZE_SVGA;
        bool known = true;
        if      (fsName == "VGA")  fs = FRAMESIZE_VGA;
        else if (fsName == "SVGA") fs = FRAMESIZE_SVGA;
        else if (fsName == "XGA")  fs = FRAMESIZE_XGA;
        else if (fsName == "HD")   fs = FRAMESIZE_HD;
        else if (fsName == "UXGA") fs = FRAMESIZE_UXGA;
        else if (fsName == "QXGA") fs = FRAMESIZE_QXGA;
        else known = false;
        if (known) cameraManager->setFrameSize(fs);
    }
    if (request->hasParam("quality", true)) {
        cameraManager->setQuality(request->getParam("quality", true)->value().toInt());
    }
    if (request->hasParam("brightness", true)) {
        cameraManager->setBrightness(request->getParam("brightness", true)->value().toInt());
    }
    if (request->hasParam("contrast", true)) {
        cameraManager->setContrast(request->getParam("contrast", true)->value().toInt());
    }
    if (request->hasParam("saturation", true)) {
        cameraManager->setSaturation(request->getParam("saturation", true)->value().toInt());
    }
    if (request->hasParam("hmirror", true)) {
        cameraManager->setHMirror(request->getParam("hmirror", true)->value().toInt() != 0);
    }
    if (request->hasParam("vflip", true)) {
        cameraManager->setVFlip(request->getParam("vflip", true)->value().toInt() != 0);
    }
    request->send(200, "application/json", "{\"status\":\"ok\",\"msg\":\"Camera settings updated\"}");
}

void WebServer::handleCapture(AsyncWebServerRequest *request) {
    camera_fb_t* fb = cameraManager->capture();
    if (!fb) {
        request->send(503, "text/plain", "Camera capture failed");
        return;
    }
    CameraFrameResponse* response = new CameraFrameResponse(cameraManager, fb);
    response->addHeader("Access-Control-Allow-Origin", "*");
    request->send(response);
}

void WebServer::handleStream(AsyncWebServerRequest *request) {
    if (!cameraManager->isInitialized()) {
        request->send(503, "text/plain", "Camera not initialized");
        return;
    }
    CameraStreamResponse* response = new CameraStreamResponse(cameraManager);
    response->addHeader("Access-Control-Allow-Origin", "*");
    request->send(response);
}

void WebServer::handleSaveLight(AsyncWebServerRequest *request) {
    if (!request->hasParam("on", true)) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Missing on parameter\"}");
        return;
    }
    bool on = request->getParam("on", true)->value().toInt() != 0;
    stripLedManager->setRingLight(on);
    String msg = on ? "Ring light turned on" : "Ring light turned off";
    StaticJsonDocument<128> doc;
    doc["status"] = "ok";
    doc["msg"] = msg;
    doc["ringLightOn"] = on;
    String out; serializeJson(doc, out);
    request->send(200, "application/json", out);
}

void WebServer::handleSendStyle(AsyncWebServerRequest *request) {
    String html = R"(
    body{font-family:Arial,sans-serif;background:#f4f4f9;color:#333;margin:0;padding:20px}
h1{text-align:center;color:#4CAF50}
h2{color:#4CAF50;font-size:1.05em;margin:0 0 14px}
a{text-decoration:none;color:#4CAF50}
a:hover{text-decoration:underline}
.back{display:block;text-align:center;margin:15px}
.menu{list-style:none;padding:0;max-width:400px;margin:20px auto}
.menu li{margin:10px 0}
.menu a{display:block;text-align:center;padding:10px;background:#4CAF50;color:#fff;text-decoration:none;border-radius:4px}
.menu a:hover{background:#45a049}
#notification{max-width:400px;display:none;padding:10px;margin:15px auto;border-radius:5px;font-size:14px;text-align:center}
.card{max-width:400px;margin:15px auto;background:#fff;padding:20px;border-radius:8px;box-shadow:0 2px 6px rgba(0,0,0,.12)}
.fg{margin-bottom:14px}
label{display:block;margin-bottom:4px;font-weight:bold;font-size:14px}
input[type=text],input[type=password],input[type=number]{width:calc(100% - 18px);padding:8px;border:1px solid #ccc;border-radius:4px;font-size:14px}
input[type=checkbox]{margin-right:6px}
button{width:100%;padding:10px;background:#4CAF50;color:#fff;border:none;border-radius:4px;cursor:pointer;font-size:15px;margin-top:6px}
button:hover{background:#45a049}
.btn-blue{background:#1976D2}
.btn-blue:hover{background:#1565C0}
.alert{padding:10px;margin:15px auto;border-radius:5px;font-size:14px;text-align:center;max-width:400px}
.alert-info{background:#cce5ff;color:#004085}
.alert-warn{background:#fff3cd;color:#856404}
.badge{display:inline-block;padding:2px 9px;border-radius:10px;font-size:13px;font-weight:bold}
.badge-ok{background:#d4edda;color:#155724}
.badge-err{background:#f8d7da;color:#721c24}
.badge-warn{background:#fff3cd;color:#856404}
.progress{background:#e0e0e0;border-radius:4px;height:18px;overflow:hidden}
.progress-bar{height:100%;background:#4CAF50;border-radius:4px;transition:width .4s}
.footer{max-width:400px;margin:10px auto;font-size:12px;color:#888;text-align:center}
.footer a{color:#4CAF50}

    )";
    request->send(200, "text/css", html);
}

void WebServer::handleSendJS(AsyncWebServerRequest *request) {
    String html = R"(
    function showNotification(msg,ok){
  var n=document.getElementById('notification');
  if(!n)return;
  n.innerText=msg;
  n.style.background=ok?'#4CAF50':'#e53935';
  n.style.color='#fff';
  n.style.display='block';
  if(ok)setTimeout(function(){n.style.display='none'},3000);
}
function apiPost(url,data,cb){
  var body=Object.keys(data).map(function(k){
    return encodeURIComponent(k)+'='+encodeURIComponent(data[k]);
  }).join('&');
  fetch(url,{method:'POST',headers:{'Content-Type':'application/x-www-form-urlencoded'},body:body})
    .then(function(r){return r.json()})
    .then(function(d){
      if(d.status==='ok'){showNotification(d.msg||'Saved',true);if(cb)cb(d);}
      else showNotification(d.msg||'Error',false);
    })
    .catch(function(e){showNotification('Error: '+e.message,false);});
}
)";
    request->send(200, "application/javascript", html);
}

void WebServer::handleNotFound(AsyncWebServerRequest *request) {
    request->send(404, "text/plain", "Page not found");
}

void WebServer::stop() {
    server->end();
}