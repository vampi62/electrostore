void handleMenuWifi(AsyncWebServerRequest *request) {
  Serial.println("wifiPageLoading");
  // si ssid contient "�" alors ssid = ""
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Paramètres WiFi</h1>";
  response += "<form action='/savewifi' method='get'>";
  response += "SSID: <input type='text' name='ssid' value='" + ssid + "'><br>";
  if (password.length() > 0) {
    response += "Mot de passe: <input type='password' name='password' placeholder='********'><br>";
  } else {
    response += "Mot de passe: <input type='password' name='password'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  request->send(200, "text/html", response);
  Serial.println("wifiPageLoading");
}

void handleSaveWifi(AsyncWebServerRequest *request) {
  String newSSID = request->arg("ssid");
  String newPassword = request->arg("password");
  bool FormChange = false;
  if (newSSID.length() > 0) {
    if (ssid != newSSID){
      ssid = newSSID;
      writeStringToEEPROM(SSID_ADDRESS, ssid);
      FormChange = true;
    }
  }
  if (newPassword.length() > 0) {
    if (password != newPassword){
      password = newPassword;
      writeStringToEEPROM(PASSWORD_ADDRESS, password);
      FormChange = true;
    }
  }
  if (FormChange) {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='10;url=/menuwifi'></head><body>";
    response += "Paramètres enregistrés. Redémarrage du module.";
    response += "</body></html>";
    request->send(200, "text/html", response);
    ESP.restart();
  } else {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='3;url=/menuwifi'></head><body>";
    response += "Erreur: pas de changement.";
    response += "</body></html>";
    request->send(400, "text/html", response);
  }
}

void handleMenuCam(AsyncWebServerRequest *request) {
  Serial.println("camPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Paramètres CAM</h1>";
  response += "<form action='/savecam' method='get'>";
  response += "CAM User: <input type='text' name='camuser' value='" + camUser + "'><br>";
  if (camPassword.length() > 0) {
    response += "CAM Password: <input type='password' name='campassword' placeholder='********'><br>";
  } else {
    response += "CAM Password: <input type='password' name='campassword'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  request->send(200, "text/html", response);
  Serial.println("camPageSend");
}

void handleSaveCam(AsyncWebServerRequest *request) {
  String newCamUser = request->arg("camuser");
  String newCamPassword = request->arg("campassword");
  bool FormChange = false;
  if (newCamUser.length() > 0) {
    if (camUser != newCamUser){
      camUser = newCamUser;
      writeStringToEEPROM(CAMUSER_ADDRESS, camUser);
      FormChange = true;
    }
  }
  if (newCamPassword.length() > 0) {
    if (camPassword != newCamPassword){
      camPassword = newCamPassword;
      writeStringToEEPROM(CAMPASSWORD_ADDRESS, camPassword);
      FormChange = true;
    }
  }
  if (FormChange) {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='10;url=/menucam'></head><body>";
    response += "Paramètres enregistrés. Redémarrage du module.";
    response += "</body></html>";
    request->send(200, "text/html", response);
  } else {
    String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='3;url=/menucam'></head><body>";
    response += "Erreur: pas de changement.";
    response += "</body></html>";
    request->send(400, "text/html", response);
  }
}

void handleLight(AsyncWebServerRequest *request) {
  // si un user et un mot de passe sont définis
  if (!authenticate(request)) {
    return;
  }
  String lightState = request->arg("state");
  if (lightState == "on") {
    for (int i = 1; i < 64; i++) {
      strip.setPixelColor(i, strip.Color(50, 50, 50));
    }
    ringLightPower = 50;
  } else {
    for (int i = 1; i < 64; i++) {
      strip.setPixelColor(i, strip.Color(0, 0, 0));
    }
    ringLightPower = 0;
  }
  strip.show();
  // Retourner un message de confirmation
  String response = "<html><head><meta charset='UTF-8'><meta http-equiv='refresh' content='3;url=/menucam'></head><body>";
  response += "Lumière " + lightState;
  response += "</body></html>";
  request->send(200, "text/html", response);
}

void handleStatus(AsyncWebServerRequest *request) {
  // si un user et un mot de passe sont définis
  if (!authenticate(request)) {
    return;
  }
  StaticJsonDocument<200> doc;
  doc["uptime"] = millis() / 1000;
  doc["ringLightPower"] = ringLightPower;

  String jsonResponse;
  serializeJson(doc, jsonResponse);
  request->send(200, "application/json", jsonResponse);
}

void handleRoot(AsyncWebServerRequest *request) {
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