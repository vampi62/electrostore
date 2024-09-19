void handleMenuWifi() {
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
  server.send(200, "text/html", response);
  Serial.println("wifiPageLoading");
}

void handleSaveWifi() {
  String newSSID = server.arg("ssid");
  String newPassword = server.arg("password");
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
    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
    ESP.restart();
  } else {
    server.send(400, "text/plain", "Erreur: pas de changement.");
  }
}

void handleMenuMqtt() {
  Serial.println("mqttPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Paramètres MQTT</h1>";
  response += "<form action='/savemqtt' method='get'>";
  response += "MQTT Server: <input type='text' name='mqttserver' value='" + mqttServer + "'><br>";
  response += "MQTT Port: <input type='text' name='mqttport' value='" + mqttPort + "'><br>";
  response += "MQTT Name: <input type='text' name='mqttname' value='" + mqttname + "'><br>";
  response += "MQTT User: <input type='text' name='mqttuser' value='" + mqttUser + "'><br>";
  if (mqttPassword.length() > 0) {
    response += "MQTT Password: <input type='password' name='mqttpassword' placeholder='********'><br>";
  } else {
    response += "MQTT Password: <input type='password' name='mqttpassword'><br>";
  }
  response += "MQTT Topic: <input type='text' name='mqtttopic' value='" + mqttTopic + "'><br>";
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  server.send(200, "text/html", response);
  Serial.println("mqttPageSend");
}

void handleSaveMqtt() {
  String newMqttServer = server.arg("mqttserver");
  String newMqttPort = server.arg("mqttport");
  String newMqttName = server.arg("mqttname");
  String newMqttUser = server.arg("mqttuser");
  String newMqttPassword = server.arg("mqttpassword");
  String newMqttTopic = server.arg("mqtttopic");
  bool FormChange = false;
  if (newMqttServer.length() > 0) {
    if (mqttServer != newMqttServer){
      mqttServer = newMqttServer;
      writeStringToEEPROM(MQTTSERVER_ADDRESS, mqttServer);
      FormChange = true;
    }
  }
  if (newMqttPort.length() > 0) {
    if (mqttPort != newMqttPort){
      mqttPort = newMqttPort;
      writeStringToEEPROM(MQTTPORT_ADDRESS, mqttPort);
      FormChange = true;
    }
  }
  if (newMqttName.length() > 0) {
    if (mqttname != newMqttName){
      mqttname = newMqttName;
      writeStringToEEPROM(MQTTNAME_ADDRESS, mqttname);
      FormChange = true;
    }
  }
  if (newMqttUser.length() > 0) {
    if (mqttUser != newMqttUser){
      mqttUser = newMqttUser;
      writeStringToEEPROM(MQTTUSER_ADDRESS, mqttUser);
      FormChange = true;
    }
  }
  if (newMqttPassword.length() > 0) {
    if (mqttPassword != newMqttPassword){
      mqttPassword = newMqttPassword;
      writeStringToEEPROM(MQTTPASSWORD_ADDRESS, mqttPassword);
      FormChange = true;
    }
  }
  if (newMqttTopic.length() > 0) {
    if (mqttTopic != newMqttTopic){
      mqttTopic = newMqttTopic;
      writeStringToEEPROM(MQTTTOPIC_ADDRESS, mqttTopic);
      FormChange = true;
    }
  }
  if (FormChange) {
    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
    ESP.restart();
  } else {
    server.send(400, "text/plain", "Erreur: pas de changement.");
  }
}

void handleRoot() {
  Serial.println("rootPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Menu</h1>";
  response += "<ul>";
  response += "<li><a href='/menuwifi'>Paramètres WiFi</a></li>";
  response += "<li><a href='/menumqtt'>Paramètres MQTT</a></li>";
  response += "</ul>";
  response += "</body></html>";
  server.send(200, "text/html", response);
  Serial.println("rootPageSend");
}