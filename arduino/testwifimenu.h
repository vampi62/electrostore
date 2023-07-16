void handleMenuWifi() {
  String response = "<html><body>";
  response += "<h1>Paramètres WiFi</h1>";
  response += "<form action='/savewifi' method='get'>";
  response += "SSID: <input type='text' name='ssid'><br>";
  response += "Mot de passe: <input type='text' name='password'><br>";
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  server.send(200, "text/html", response);
}

void handleSaveWifi() {
  String newSSID = server.arg("ssid");
  String newPassword = server.arg("password");
  if (newSSID.length() > 0 && newPassword.length() > 0) {
    ssid = newSSID;
    password = newPassword;

    // Écrire les nouveaux paramètres dans l'EEPROM
    writeStringToEEPROM(SSID_ADDRESS, ssid);
    writeStringToEEPROM(PASSWORD_ADDRESS, password);

    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
    ESP.restart();
  } else {
    server.send(400, "text/plain", "Erreur: SSID ou mot de passe manquant.");
  }
}

void handleMenuMqtt() {
  String response = "<html><body>";
  response += "<h1>Paramètres MQTT</h1>";
  response += "<form action='/savemqtt' method='get'>";
  response += "MQTT Server: <input type='text' name='mqttserver' value='" + mqttServer + "'><br>";
  response += "MQTT Port: <input type='text' name='mqttport' value='" + mqttPort + "'><br>";
  response += "MQTT Name: <input type='text' name='mqttname' value='" + mqttname + "'><br>";
  response += "MQTT User: <input type='text' name='mqttuser' value='" + mqttUser + "'><br>";
  response += "MQTT Password: <input type='text' name='mqttpassword' value='" + mqttPassword + "'><br>";
  response += "MQTT Topic: <input type='text' name='mqtttopic' value='" + mqttTopic + "'><br>";
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  server.send(200, "text/html", response);
}

void handleSaveMqtt() {
  String newMqttServer = server.arg("mqttserver");
  String newMqttPort = server.arg("mqttport");
  String newMqttName = server.arg("mqttname");
  String newMqttUser = server.arg("mqttuser");
  String newMqttPassword = server.arg("mqttpassword");
  String newMqttTopic = server.arg("mqtttopic");
  if (newMqttServer.length() > 0 && newMqttPort.length() > 0 && newMqttName.length() > 0 && newMqttUser.length() > 0 && newMqttPassword.length() > 0 && newMqttTopic.length() > 0) {
    mqttServer = newMqttServer;
    mqttPort = newMqttPort;
    mqttname = newMqttName;
    mqttUser = newMqttUser;
    mqttPassword = newMqttPassword;
    mqttTopic = newMqttTopic;

    // Écrire les nouveaux paramètres dans l'EEPROM
    writeStringToEEPROM(MQTTSERVER_ADDRESS, mqttServer);
    writeStringToEEPROM(MQTTPORT_ADDRESS, mqttPort);
    writeStringToEEPROM(MQTTNAME_ADDRESS, mqttname);
    writeStringToEEPROM(MQTTUSER_ADDRESS, mqttUser);
    writeStringToEEPROM(MQTTPASSWORD_ADDRESS, mqttPassword);
    writeStringToEEPROM(MQTTTOPIC_ADDRESS, mqttTopic);

    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
    ESP.restart();
  } else {
    server.send(400, "text/plain", "Erreur: paramètre manquant.");
  }
}

void handleRoot() {
  String response = "<html><body>";
  response += "<h1>Menu</h1>";
  response += "<ul>";
  response += "<li><a href='/menuwifi'>Paramètres WiFi</a></li>";
  response += "<li><a href='/menumqtt'>Paramètres MQTT</a></li>";
  response += "</ul>";
  response += "</body></html>";
  server.send(200, "text/html", response);
}