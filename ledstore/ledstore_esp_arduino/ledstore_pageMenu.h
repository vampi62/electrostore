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

void handleStatus(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  StaticJsonDocument<500> doc;
  doc["uptime"] = millis() / 1000;
  doc["espModel"] = ESP.getChipId();
  doc["versionLedStore"] = version_ledstore;
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
  response += "<li><a href='/wifi'>WiFi Settings</a></li>";
  response += "<li><a href='/user'>User Settings</a></li>";
  response += "<li><a href='/mqtt'>Mqtt Settings</a></li>";
  response += "</ul>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("rootPageSend");
}