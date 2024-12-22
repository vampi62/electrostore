bool setupWiFi()
{
  Serial.println();
  Serial.print("Connexion au réseau Wi-Fi: ");
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
      Serial.println("Wi-Fi connecté !");
      Serial.print("Adresse IP: ");
      Serial.println(WiFi.localIP());
      nbrErreurWifiConnect = 0;
    }
    else
    {
      Serial.println("");
      Serial.println("Connexion au réseau Wi-Fi échouée.");
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
    Serial.print("Adresse IP du réseau temporaire: ");
    Serial.println(WiFi.softAPIP());
    return false;
  }
}