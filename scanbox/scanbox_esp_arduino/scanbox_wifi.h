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