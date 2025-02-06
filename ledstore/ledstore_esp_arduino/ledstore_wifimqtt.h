bool setupWiFi()
{
  Serial.println();
  Serial.print("Connecting to Wi-Fi network: ");
  Serial.println(wifiSSID);
  WiFi.mode(WIFI_STA);
  do
  {
    WiFi.begin(wifiSSID.c_str(), wifiPassword.c_str());
    startTime = millis();
    while (WiFi.status() != WL_CONNECTED && millis() - startTime < connectionTimeout)
    {
      delay(500);
      Serial.print(".");
    }
    if (WiFi.status() == WL_CONNECTED)
    {
      Serial.println("");
      Serial.println("Wi-Fi connected!");
      Serial.print("IP Address: ");
      Serial.println(WiFi.localIP());
      nbrErreurWifiConnect = 0;
    }
    else
    {
      Serial.println("");
      Serial.println("Wi-Fi connection failed.");
      nbrErreurWifiConnect++;
    }
  } while ((WiFi.status() != WL_CONNECTED) && (nbrErreurWifiConnect <= 3));
  if (WiFi.status() == WL_CONNECTED)
  {
    return true;
  }
  else
  {
    WiFi.mode(WIFI_AP);
    WiFi.softAP(ap_ssid, ap_password);
    Serial.print("Temporary network IP address: ");
    Serial.println(WiFi.softAPIP());
    return false;
  }
}

bool reconnectMQTT()
{
  // Connexion au serveur MQTT
  if (nbrErreurMqttConnect > 3)
  {
    return false;
  }
  String macAddress = WiFi.macAddress();
  macAddress.replace(":", "");
  String SessionName = mqttName + macAddress.substring(macAddress.length()-6);
  Serial.println();
  Serial.print("Connecting to MQTT server...");
  Serial.println(SessionName);
  do
  {
    if (mqttClient.connect(SessionName.c_str(), mqttUser.c_str(), mqttPassword.c_str()))
    {
      Serial.println("connected!");
      Serial.println(mqttTopic.c_str());
      mqttClient.subscribe(mqttTopic.c_str());
      strip.setPixelColor(0, strip.Color(0, 20, 0));
      strip.show();
      nbrErreurMqttConnect = 0;
      delay(1000);
	  strip.setPixelColor(0, strip.Color(0, 0, 0));
	  strip.show();
    }
    else
    {
      Serial.print("Failed, error code = ");
      Serial.println(mqttClient.state());
      strip.setPixelColor(0, strip.Color(20, 20, 0));
      strip.show();
      nbrErreurMqttConnect++;
      delay(1500);
    }
    delay(10);
  } while ((!mqttClient.connected()) && (nbrErreurMqttConnect <= 3));
  return mqttClient.connected();
}