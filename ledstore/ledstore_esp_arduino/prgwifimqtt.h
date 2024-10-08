bool setupWiFi() {
  Serial.println();
  Serial.print("Connexion au réseau Wi-Fi: ");
  Serial.println(ssid);
  WiFi.mode(WIFI_STA);
  do {
    WiFi.begin(ssid.c_str(), password.c_str());
    startTime = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - startTime < connectionTimeout) {
      delay(500);
      Serial.print(".");
    }
    if (WiFi.status() == WL_CONNECTED) {
      Serial.println("");
      Serial.println("Wi-Fi connecté !");
      Serial.print("Adresse IP: ");
      Serial.println(WiFi.localIP());
      nbrErreurWifiConnect = 0;
    } else {
      Serial.println("");
      Serial.println("Connexion au réseau Wi-Fi échouée.");
      nbrErreurWifiConnect++;
    }
  } while ((WiFi.status() != WL_CONNECTED) && (nbrErreurWifiConnect <= 5));
  if (WiFi.status() == WL_CONNECTED) {
    return true;
  } else {
    WiFi.mode(WIFI_AP);
    WiFi.softAP(ap_ssid, ap_password);
    Serial.print("Adresse IP du réseau temporaire: ");
    Serial.println(WiFi.softAPIP());
    return false;
  }
}

bool reconnectMQTT() {
  // Connexion au serveur MQTT
  if (nbrErreurMqttConnect > 5){
    return false;
  }
  Serial.println();
  Serial.print("Connexion au serveur MQTT...");
  Serial.println(mqttname);
  startTime = millis();
  do {
    if (mqttClient.connect(mqttname.c_str(), mqttUser.c_str(), mqttPassword.c_str())) {
      Serial.println("connecté !");
      Serial.println(mqttTopic.c_str());
      mqttClient.subscribe(mqttTopic.c_str());
      strip.setPixelColor(0, strip.Color(0, 20, 0));
      strip.show();
      nbrErreurMqttConnect = 0;
    } else {
      Serial.println("échec, code d'erreur = ");
      Serial.print(mqttClient.state());
      strip.setPixelColor(0, strip.Color(20, 20, 0));
      strip.show();
      nbrErreurMqttConnect++;
      delay(1500);
    }
    delay(10);
  } while ((!mqttClient.connected()) && (nbrErreurMqttConnect <= 5));
  return mqttClient.connected();
}