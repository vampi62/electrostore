void handleMenuWifi(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  Serial.println("wifiPageLoading");
  // si ssid contient "�" alors ssid = ""
  String response = "<html><head><meta charset='UTF-8'>";
  response += "<script>";
  response += "setInterval(function() {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('wifiSignalStrength').innerText = data.wifiSignalStrength + ' dBm';";
  response += "  });";
  response += "}, 5000);";
  response += "</script>";
  response += "</head><body>";
  response += "<h1>Paramètres WiFi</h1>";
  response += "<form action='/savewifi' method='get'>";
  response += "SSID: <input type='text' name='ssid' value='" + ssid + "'><br>";
  if (password.length() > 0)
  {
    response += "Mot de passe: <input type='password' name='password' placeholder='********'><br>";
  }
  else
  {
    response += "Mot de passe: <input type='password' name='password'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  // show the current wifi signal strength and other data
  response += "<div>";
  response += "<b>Adresse IP:</b> ";
  if (WiFi.getMode() != WIFI_AP)
  {
    response += WiFi.localIP().toString() + "<br>";
  }
  else
  {
    response += WiFi.softAPIP().toString() + "<br>";
  }
  response += "<b>Adresse MAC:</b> " + WiFi.macAddress() + "<br>";
  response += "<b>SSID:</b> " + WiFi.SSID() + "<br>";
  response += "<b>Force du signal:</b> <span id='wifiSignalStrength'>" + String(WiFi.RSSI()) + " dBm</span><br>";
  response += "<b>Wifi Mode:</b> ";
  if (WiFi.getMode() != WIFI_AP)
  {
    response += "Station<br>";
  }
  else
  {
    response += "Point d'accès<br>";
  }
  response += "</div>";
  response += "</body></html>";
  request->send(200, "text/html", response);
  Serial.println("wifiPageLoading");
}

void handleSaveWifi(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String newSSID = request->arg("ssid");
  String newPassword = request->arg("password");
  bool FormChange = false;
  if (newSSID.length() > 0)
  {
    if (ssid != newSSID)
    {
      ssid = newSSID;
      writeStringToEEPROM(SSID_ADDRESS, ssid);
      FormChange = true;
    }
  }
  if (newPassword.length() > 0)
  {
    if (password != newPassword)
    {
      password = newPassword;
      writeStringToEEPROM(PASSWORD_ADDRESS, password);
      FormChange = true;
    }
  }
  if (FormChange)
  {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='10;url=/menuwifi'></head><body>";
    response += "Paramètres enregistrés. Redémarrage du module.";
    response += "</body></html>";
    request->send(200, "text/html", response);
    ESP.restart();
  }
  else
  {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='3;url=/menuwifi'></head><body>";
    response += "Erreur: pas de changement.";
    response += "</body></html>";
    request->send(400, "text/html", response);
  }
}

void handleMenuCam(AsyncWebServerRequest *request)
{
  Serial.println("camPageLoading");
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String response = "<html><head><meta charset='UTF-8'>";
  response += "<script>";
  response += "setInterval(function() {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('espTemperature').innerText = data.espTemperature + '°C';";
  response += "    document.getElementById('uptime').innerText = data.uptime + 's';";
  response += "  });";
  response += "}, 5000);";
  response += "</script>";

  response += "<h1>Paramètres CAM</h1>";
  response += "<form action='/savecam' method='get'>";
  response += "CAM User: <input type='text' name='camuser' value='" + camUser + "'><br>";
  if (camPassword.length() > 0)
  {
    response += "CAM Password: <input type='password' name='campassword' placeholder='********'><br>";
  }
  else
  {
    response += "CAM Password: <input type='password' name='campassword'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "<div>";
  // connect to the camera stream
  response += "<img src='/stream' width='640' height='480'><br>";
  response += "<b>Version ScanBox:</b> " + String(version_scanbox) + "<br>";
  response += "<b>Résolution de la caméra:</b> " + camResolution + "<br>";
  sensor_t *s = esp_camera_sensor_get();
  response += "<b>Camera PID:</b> " + String(s->id.PID) + "<br>";
  response += "<b>Temperature ESP:</b> <span id='espTemperature'>" + String(temperatureRead()) + "°C</span><br>";
  response += "<b>ESP Model:</b> " + String(ESP.getChipModel()) + "<br>";
  response += "<b>ESP Uptime:</b> <span id='uptime'>" + String(millis() / 1000) + "s</span><br>";
  response += "<br>";
  response += "<label for='lightSwitch'>Ring Light:</label> <input type='checkbox' id='lightSwitch' onchange='toggleLight()'><br>";
  response += "<input type='range' id='ringLightPower' name='ringLightPower' min='0' max='255' value='" + String(ringLightPower) + "' onchange='updateLight(this.value)'>";
  response += "<script>";
  response += "let lastValue = " + String(ringLightPower) + ";";
  response += "document.getElementById('lightSwitch').checked = " + String((ringLightPower > 0 ? "true" : "false")) + ";";
  response += "document.getElementById('ringLightPower').style.display = " + String((ringLightPower > 0 ? "'block'" : "'none'")) + ";";
  response += "function toggleLight() {";
  response += "  const rangeInput = document.getElementById('ringLightPower');";
  response += "  const lightSwitch = document.getElementById('lightSwitch');";
  response += "  if (lightSwitch.checked) {";
  response += "    rangeInput.style.display = 'block';";
  response += "    updateLight(lastValue);";
  response += "  } else {";
  response += "    lastValue = rangeInput.value;";
  response += "    rangeInput.style.display = 'none';";
  response += "    updateLight(0);";
  response += "  }";
  response += "}";
  response += "function updateLight(val) {";
  response += "  let xhr = new XMLHttpRequest();";
  response += "  xhr.open('GET', '/light?ringLightPower=' + val, true);";
  response += "  xhr.send();";
  response += "}";
  response += "</script>";

  response += "<br>";
  response += "</div>";
  response += "</body></html>";
  request->send(200, "text/html", response);
  Serial.println("camPageSend");
}

void handleSaveCam(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String newCamUser = request->arg("camuser");
  String newCamPassword = request->arg("campassword");
  bool FormChange = false;
  if (newCamUser.length() > 0)
  {
    if (camUser != newCamUser)
    {
      camUser = newCamUser;
      writeStringToEEPROM(CAMUSER_ADDRESS, camUser);
      FormChange = true;
    }
  }
  if (newCamPassword.length() > 0)
  {
    if (camPassword != newCamPassword)
    {
      camPassword = newCamPassword;
      writeStringToEEPROM(CAMPASSWORD_ADDRESS, camPassword);
      FormChange = true;
    }
  }
  if (FormChange)
  {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='10;url=/menucam'></head><body>";
    response += "Paramètres enregistrés. Redémarrage du module.";
    response += "</body></html>";
    request->send(200, "text/html", response);
  }
  else
  {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='3;url=/menucam'></head><body>";
    response += "Erreur: pas de changement.";
    response += "</body></html>";
    request->send(400, "text/html", response);
  }
}

void handleLight(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  int newRingLightPower = request->arg("ringLightPower") != "" ? request->arg("ringLightPower").toInt() : 0;
  if (newRingLightPower > 255)
  {
    newRingLightPower = 255;
  }
  if (newRingLightPower < 0)
  {
    newRingLightPower = 0;
  }
  for (int i = 1; i < 64; i++)
  {
    strip.setPixelColor(i, strip.Color(newRingLightPower, newRingLightPower, newRingLightPower));
  }
  strip.show();
  ringLightPower = newRingLightPower;
  // Retourner un message de confirmation
  StaticJsonDocument<200> doc;
  doc["ringLightPower"] = ringLightPower;
  String jsonResponse;
  serializeJson(doc, jsonResponse);
  request->send(200, "application/json", jsonResponse);
}

void handleStatus(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  sensor_t *s = esp_camera_sensor_get();
  StaticJsonDocument<500> doc;
  doc["uptime"] = millis() / 1000;
  doc["espModel"] = ESP.getChipModel();
  doc["espTemperature"] = temperatureRead();
  doc["ringLightPower"] = ringLightPower;
  doc["versionScanBox"] = version_scanbox;
  doc["cameraResolution"] = camResolution;
  doc["cameraPID"] = s->id.PID;
  doc["wifiSignalStrength"] = String(WiFi.RSSI());
  String jsonResponse;
  serializeJson(doc, jsonResponse);
  request->send(200, "application/json", jsonResponse);
}

void handleRoot(AsyncWebServerRequest *request)
{
  Serial.println("rootPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Menu</h1>";
  response += "<ul>";
  response += "<li><a href='/menuwifi'>Paramètres WiFi</a></li>";
  response += "<li><a href='/menucam'>Paramètres CAM</a></li>";
  response += "</ul>";
  response += "</body></html>";
  request->send(200, "text/html", response);
  Serial.println("rootPageSend");
}