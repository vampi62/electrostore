#include "WebServer.h"
#include "Logger.h"
#include "config.h"
#include "StorageManager.h"

WebServer::WebServer(WiFiManager* wm, MQTTManager* mm, OTAManager* om) : wifiManager(wm), mqttManager(mm), otaManager(om) {
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
    // En mode AP, l'authentification est désactivée pour permettre la réinitialisation
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
    // Page Menu
    server->on("/", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleRoot(request);
    });

    // Status JSON
    server->on("/status", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleStatus(request);
    });

    // Sauvegarde WiFi
    server->on("/wifi", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleWiFiPage(request);
    });

    // Sauvegarde WiFi
    server->on("/wifi", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveWiFi(request);
    });

    // Page gestion des identifiants
    server->on("/auth", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleAuthPage(request);
    });

    // Sauvegarde des identifiants
    server->on("/auth", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveAuth(request);
    });

    // Page OTA
    server->on("/ota", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleOTAPage(request);
    });

    // Sauvegarde OTA
    server->on("/ota", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveOTA(request);
    });

    // Page MQTT
    server->on("/mqtt", HTTP_GET, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleMQTTPage(request);
    });

    // Sauvegarde MQTT
    server->on("/mqtt", HTTP_POST, [this](AsyncWebServerRequest *request) {
        if (!authenticate(request)) return;
        handleSaveMQTT(request);
    });
    
    // Fichiers statiques (CSS, JS)
    server->on("/style.css", HTTP_GET, [this](AsyncWebServerRequest *request) {
        handleSendStyle(request);
    });
    
    // Fichiers statiques (CSS, JS)
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
<html lang='fr'>
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
        .info a { text-align: center; margin: 3px; text-decoration: none; color: #4CAF50; }
        .info a:hover { text-decoration: underline; }
    </style>
</head>
<body>
    <h1>Menu</h1>
    <ul class='menu'>
        <li><a href='/wifi'>WiFi Settings</a></li>
        <li><a href='/auth'>User Settings</a></li>
        <li><a href='/mqtt'>Mqtt Settings</a></li>
        <li><a href='/ota'>OTA Settings</a></li>
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
#if defined(ESP32)
    doc["espModel"] = ESP.getChipModel();
#elif defined(ESP8266)
    doc["espModel"] = ESP.getChipId();
#endif
    doc["OTAWait"] = otaManager->isWindowOpen();
    doc["OTAUploading"] = otaManager->isUpdating();
    doc["OTAError"] = otaManager->getLastError();
    doc["OTATime"] = OTA_WINDOW_MS / 1000;
    doc["OTARemainingTime"] = otaManager->getRemainingTime();
    doc["OTAPercentage"] = otaManager->getProgress();
    doc["versionLedStore"] = VERSION;
    doc["wifiSignalStrength"] = String(WiFi.RSSI());
#if defined(ESP32)
    doc["WifiConnectionMode"] = (WiFi.getMode() == WIFI_MODE_AP) ? "AP" : "STA";
#elif defined(ESP8266)
    doc["WifiConnectionMode"] = (WiFi.getMode() == WIFI_AP) ? "AP" : "STA";
#endif
    doc["wifiSSID"] = WiFi.SSID();
    doc["wifiIP"] = WiFi.localIP().toString();
    doc["wifiStatus"] = (WiFi.status() == WL_CONNECTED) ? "Connected" : "Disconnected";
    doc["wifiMAC"] = WiFi.macAddress();
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(200, "application/json", jsonResponse);
}

void WebServer::handleWiFiPage(AsyncWebServerRequest *request) {
    String curSsid = wifiManager->getSsid();
    bool hasPwd = wifiManager->hasPassword();
    String html =
        "<!DOCTYPE html><html lang='fr'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>WiFi Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>WiFi Settings</h1><div id='notification'></div>"
        "<div class='card'>"
        "<div class='fg'><label>SSID</label>"
        "<input type='text' id='ssid' value='" + curSsid + "' maxlength='50'></div>"
        "<div class='fg'><input type='checkbox' id='usePwd' onchange='tglPwd()' " +
        String(hasPwd ? "checked" : "") +
        "> Mot de passe requis</div>"
        "<div class='fg'><label>Mot de passe</label>"
        "<input type='password' id='pwd' maxlength='50' " +
        String(hasPwd ? "placeholder='&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;'" : "disabled") +
        "></div>"
        "<button onclick='save()'>Enregistrer &amp; Red&#233;marrer</button></div>"
        "<a class='back' href='/'>&#8592; Retour</a>"
        "<div class='card'>"
        "<b>IP :</b> " + WiFi.localIP().toString() +
        " &nbsp; <b>MAC :</b> " + WiFi.macAddress() +
        "<br><b>SSID actuel :</b> " + WiFi.SSID() +
        " &nbsp; <b>Signal :</b> <span id='rssi'>" + String(WiFi.RSSI()) + " dBm</span>"
#if defined(ESP32)
        "<br><b>Mode :</b> " + String(WiFi.getMode() == WIFI_MODE_AP ? "AP" : "STA") +
#elif defined(ESP8266)
        "<br><b>Mode :</b> " + String(WiFi.getMode() == WIFI_AP ? "AP" : "STA") +
#endif
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
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Param\\u00e8tre ssid manquant\"}");
        return;
    }
    String ssid     = request->getParam("ssid", true)->value();
    String password = request->hasParam("password", true) ? request->getParam("password", true)->value() : "";
    wifiManager->saveCredentials(ssid, password);
    request->send(200, "application/json", "{\"status\":\"ok\",\"msg\":\"Credentials sauvegard\\u00e9s. Red\\u00e9marrage...\"}");
    delay(1000);
    ESP.restart();
}

void WebServer::handleAuthPage(AsyncWebServerRequest *request) {
    bool hasCred = (espPassword.length() > 0);
    bool isAP    = (wifiManager->getCurrentMode() == WIFI_CONN_AP);
    String html =
        "<!DOCTYPE html><html lang='fr'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>Gestion des acc&#232;s</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>Gestion des acc&#232;s</h1><div id='notification'></div>";
    if (isAP)
        html += "<div class='alert alert-warn'>&#9888; Mode AP : authentification d&#233;sactiv&#233;e.</div>";
    else if (!hasCred)
        html += "<div class='alert alert-info'>&#9432; Aucun identifiant configur&#233;. Acc&#232;s libre.</div>";
    html +=
        "<div class='card'>"
        "<div class='fg'><label>Nom d'utilisateur</label>"
        "<input type='text' id='usr' maxlength='50'></div>"
        "<div class='fg'><label>Nouveau mot de passe</label>"
        "<input type='password' id='np' maxlength='50'></div>"
        "<div class='fg'><label>Confirmer le mot de passe</label>"
        "<input type='password' id='cp' maxlength='50'></div>";
    if (hasCred && !isAP)
        html += "<div class='fg'><label>Ancien mot de passe</label>"
                "<input type='password' id='op' maxlength='50'></div>";
    html +=
        "<button onclick='save()'>Enregistrer</button></div>"
        "<a class='back' href='/'>&#8592; Retour</a>"
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
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Param\\u00e8tres manquants\"}");
        return;
    }
    String newUser  = request->getParam("user",        true)->value();
    String newPass  = request->getParam("newpass",     true)->value();
    String confPass = request->getParam("confirmpass", true)->value();
    if (newUser.indexOf(':') >= 0) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Nom d'utilisateur invalide (pas de ':')\"}");
        return;
    }
    if (newPass != confPass) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Mots de passe diff\\u00e9rents\"}");
        return;
    }
    bool isAP = (wifiManager->getCurrentMode() == WIFI_CONN_AP);
    if (espPassword.length() > 0 && !isAP) {
        if (!request->hasParam("oldpass", true)) {
            request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Ancien mot de passe requis\"}");
            return;
        }
        if (request->getParam("oldpass", true)->value() != espPassword) {
            request->send(401, "application/json", "{\"status\":\"error\",\"msg\":\"Ancien mot de passe incorrect\"}");
            return;
        }
    }
    espUser     = newUser;
    espPassword = newPass;
    StorageManager::saveAuth(espUser, espPassword);
    otaManager->setPassword(espPassword);
    request->send(200, "application/json", "{\"status\":\"ok\",\"msg\":\"Identifiants mis \\u00e0 jour avec succ\\u00e8s\"}");
}


void WebServer::handleOTAPage(AsyncWebServerRequest *request) {
    bool winOpen = otaManager->isWindowOpen();
    bool updating = otaManager->isUpdating();
    uint8_t prog = otaManager->getProgress();
    String lastErr = otaManager->getLastError();
    unsigned long rem = otaManager->getRemainingTime();
    String badge = winOpen
        ? (updating ? "<span class='badge badge-warn'>En cours</span>" : "<span class='badge badge-ok'>Ouverte</span>")
        : "<span class='badge badge-err'>Ferm&#233;e</span>";
    String html =
        "<!DOCTYPE html><html lang='fr'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>OTA Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>OTA Settings</h1><div id='notification'></div>"
        "<div class='card'><h2>Statut</h2>"
        "<p><b>Fen&#234;tre :</b> " + badge + "</p>"
        "<p><b>Restant :</b> <span id='otaRem'>" + String(rem / 1000) + "</span> s</p>"
        "<div class='progress'><div class='progress-bar' id='otaPb' style='width:" + String(prog) + "%'></div></div>";
    if (lastErr.length() > 0)
        html += "<p style='color:#e53935;font-size:13px'>" + lastErr + "</p>";
    html +=
        "<button class='btn-blue' onclick='openWin()'>Ouvrir fen&#234;tre OTA (" +
        String(OTA_WINDOW_MS / 1000) + " s)</button></div>"
        "<a class='back' href='/'>&#8592; Retour</a>"
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
    doc["msg"] = "Fen\u00eatre OTA ouverte pour " + String(OTA_WINDOW_MS / 1000) + "s";
    String out; serializeJson(doc, out);
    request->send(200, "application/json", out);
}

void WebServer::handleMQTTPage(AsyncWebServerRequest *request) {
    bool   connected = mqttManager->isConnected();
    String server    = mqttManager->getServer();
    int    port      = mqttManager->getPort();
    String user      = mqttManager->getUser();
    bool   hasPwd    = mqttManager->hasPassword();
    String topic     = mqttManager->getRawTopic();
    String badge = connected
        ? "<span class='badge badge-ok'>Connect&#233;</span>"
        : "<span class='badge badge-err'>D&#233;connect&#233;</span>";
    String html =
        "<!DOCTYPE html><html lang='fr'><head><meta charset='UTF-8'>"
        "<meta name='viewport' content='width=device-width,initial-scale=1'>"
        "<title>MQTT Settings</title><link rel='stylesheet' href='/style.css'>"
        "</head><body><h1>MQTT Settings</h1><div id='notification'></div>"
        "<div class='card'><b>Statut :</b> " + badge + "</div>"
        "<div class='card'>"
        "<div class='fg'><label>Serveur</label>"
        "<input type='text' id='ms' value='" + server + "' maxlength='100' placeholder='192.168.x.x ou hostname'></div>"
        "<div class='fg'><label>Port</label>"
        "<input type='number' id='mp' value='" + String(port > 0 ? port : 1883) + "' min='1' max='65535'></div>"
        "<div class='fg'><label>Utilisateur</label>"
        "<input type='text' id='mu' value='" + user + "' maxlength='50'></div>"
        "<div class='fg'>";
    if (hasPwd) {
        html +=
            "<input type='checkbox' id='chgPwd' onchange='tglPwd()'> Changer le mot de passe<br>"
            "<input type='password' id='mpwd' maxlength='50' "
            "placeholder='&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;&#9679;' disabled>";
    } else {
        html += "<label>Mot de passe</label><input type='password' id='mpwd' maxlength='50'>";
    }
    html +=
        "</div>"
        "<div class='fg'><label>Topic <small>(sans pr&#233;fixe &quot;" +
        String(MQTT_BASE_TOPIC) + "/&quot;)</small></label>"
        "<input type='text' id='mt' value='" + topic + "' maxlength='100'></div>"
        "<button onclick='save()'>Enregistrer &amp; Connecter</button></div>"
        "<a class='back' href='/'>&#8592; Retour</a>"
        "<script src='/common.js'></script><script>"
        "function tglPwd(){var f=document.getElementById('mpwd');"
        "f.disabled=!document.getElementById('chgPwd').checked;if(f.disabled)f.value='';}"
        "function save(){"
        "var d={server:document.getElementById('ms').value,"
        "port:document.getElementById('mp').value,"
        "user:document.getElementById('mu').value,"
        "topic:document.getElementById('mt').value};";
    if (hasPwd) {
        html +=
            "var chg=document.getElementById('chgPwd')&&document.getElementById('chgPwd').checked;"
            "if(chg){d.changePassword='1';d.password=document.getElementById('mpwd').value;}";
    } else {
        html += "d.changePassword='1';d.password=document.getElementById('mpwd').value;";
    }
    html += "apiPost('/mqtt',d);}"
            "</script></body></html>";
    request->send(200, "text/html", html);
}

void WebServer::handleSaveMQTT(AsyncWebServerRequest *request) {
    if (!request->hasParam("server", true) ||
        !request->hasParam("port",   true) ||
        !request->hasParam("topic",  true)) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Param\\u00e8tres manquants (server, port, topic)\"}");
        return;
    }
    String server = request->getParam("server", true)->value();
    int    port   = request->getParam("port",   true)->value().toInt();
    String user   = request->hasParam("user",  true) ? request->getParam("user",  true)->value() : "";
    String topic  = request->getParam("topic", true)->value();
    if (server.isEmpty()) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Serveur invalide\"}");
        return;
    }
    if (port < 1 || port > 65535) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Port invalide (1-65535)\"}");
        return;
    }
    if (topic.isEmpty()) {
        request->send(400, "application/json", "{\"status\":\"error\",\"msg\":\"Topic invalide\"}");
        return;
    }
    // Si changePassword=1, utiliser le nouveau mot de passe ; sinon conserver l'existant depuis le stockage
    String password;
    bool changePassword = request->hasParam("changePassword", true) &&
                          request->getParam("changePassword", true)->value() == "1";
    if (changePassword) {
        password = request->hasParam("password", true) ? request->getParam("password", true)->value() : "";
    } else {
        String _s, _u, _t; int _p;
        StorageManager::loadMQTTConfig(_s, _p, _u, password, _t);
    }
    mqttManager->saveCredentials(server, port, user, password, topic);
    bool ok = mqttManager->connectToMQTT(server, port, user, password, topic, MQTT_CLIENT_PREFIX);
    StaticJsonDocument<160> doc;
    doc["status"] = "ok";
    doc["msg"] = ok
        ? ("MQTT connect\u00e9 \u00e0 " + server + ":" + String(port))
        : ("Credentials sauvegard\u00e9s. Reconnexion auto dans " + String(MQTT_RECONNECT_INTERVAL / 1000) + "s");
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
.alert{padding:10px;margin:10px 0;border-radius:4px;font-size:14px}
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
      if(d.status==='ok'){showNotification(d.msg||'Enregistr\u00e9',true);if(cb)cb(d);}
      else showNotification(d.msg||'Erreur',false);
    })
    .catch(function(e){showNotification('Erreur: '+e.message,false);});
}
)";
    request->send(200, "application/javascript", html);
}

void WebServer::handleNotFound(AsyncWebServerRequest *request) {
    request->send(404, "text/plain", "Page non trouvée");
}

void WebServer::stop() {
    server->end();
}