bool setupWiFi() {
  Serial.println();
  Serial.print("Connexion au réseau Wi-Fi: ");
  Serial.println(ssid);
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
    return true;
  } else {
    Serial.println("");
    Serial.println("Connexion au réseau Wi-Fi échouée.");
    ssid = "MonReseauTemp";
    password = "MotDePasseTemp";
    WiFi.softAP(ssid.c_str(), password.c_str());
    IPAddress myIP = WiFi.softAPIP();
    Serial.print("Adresse IP du réseau temporaire: ");
    Serial.println(myIP);
    return false;
  }
}

bool reconnectMQTT() {
  // Connexion au serveur MQTT
  Serial.println();
  Serial.print("Connexion au serveur MQTT...");
  Serial.println(mqttname);
  startTime = millis();
  if (mqttClient.connect(mqttname.c_str(), mqttUser.c_str(), mqttPassword.c_str())) {
    Serial.println("connecté !");
    Serial.println(mqttTopic.c_str());
    mqttClient.subscribe(mqttTopic.c_str());
    strip.setPixelColor(0, strip.Color(0, 20, 0));
    strip.show();
  } else {
    Serial.print("échec, code d'erreur = ");
    Serial.print(mqttClient.state());
    strip.setPixelColor(0, strip.Color(20, 20, 0));
    strip.show();
  }
  delay(10);
  return mqttClient.connected();
}