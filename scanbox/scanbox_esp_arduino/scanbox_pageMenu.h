bool authenticate(AsyncWebServerRequest *request)
{
  if (espUser.length() == 0 || espPassword.length() == 0)
  {
    return true;
  }
  if (!request->authenticate(espUser.c_str(), espPassword.c_str()))
  {
    request->requestAuthentication(); // Demande d'authentification si les identifiants sont incorrects
    return false;
  }
  return true;
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
  String response = "<!DOCTYPE html>";
  response += "<html lang='fr'>";
  response += "<head>";
  response += "<meta charset='UTF-8'>";
  response += "<meta name='viewport' content='width=device-width, initial-scale=1.0'>";
  response += "<title>Menu</title>";
  response += "<style>";
  response += "  body { font-family: Arial, sans-serif; background-color: #f4f4f9; color: #333; margin: 0; padding: 20px; }";
  response += "  h1 { text-align: center; color: #4CAF50; }";
  response += "  ul { list-style-type: none; padding: 0; max-width: 400px; margin: 20px auto; }";
  response += "  li { margin: 10px 0; }";
  response += "  a { display: block; text-align: center; padding: 10px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; }";
  response += "  a:hover { background-color: #45a049; }";
  response += "</style>";
  response += "</head>";
  response += "<body>";
  response += "<h1>Menu</h1>";
  response += "<ul>";
  response += "<li><a href='/wifi'>Paramètres WiFi</a></li>";
  response += "<li><a href='/user'>Paramètres User</a></li>";
  response += "<li><a href='/cam'>Paramètres Cam</a></li>";
  response += "</ul>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("rootPageSend");
}