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
  doc["OTAWait"] = waitingOTA ? "Waiting" : "Not Waiting";
  doc["OTAUploading"] = updateOTA ? "Uploading" : "Not Uploading";
  doc["OTAError"] = updateOTAError;
  doc["OTATime"] = IntervalOTA / 1000;
  doc["OTARemainingTime"] = waitingOTA && (((IntervalOTA - (millis() - startTimeOTA)) / 1000) > 0) ? (IntervalOTA - (millis() - startTimeOTA)) / 1000 : 0;
  doc["OTAPercentage"] = otaPercentage;
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
  response += "  .menu a { display: block; text-align: center; padding: 10px; background-color: #4CAF50; color: white; text-decoration: none; border-radius: 4px; }";
  response += "  .menu a:hover { background-color: #45a049; }";
  response += "  .info a { text-align: center; margin: 3px; text-decoration: none; color: #4CAF50; }";
  response += "  .info a:hover { text-decoration: underline; }";
  response += "</style>";
  response += "</head>";
  response += "<body>";
  response += "<h1>Menu</h1>";
  response += "<ul class='menu'>";
  response += "<li><a href='/wifi'>WiFi Settings</a></li>";
  response += "<li><a href='/user'>User Settings</a></li>";
  response += "<li><a href='/cam'>Camera Settings</a></li>";
  response += "<li><a href='/ota'>OTA Settings</a></li>";
  response += "</ul>";
  response += "<div class='info'>";
  response += "<b>ScanBox Version:</b> " + String(version_scanbox) + "<br>";
  response += "<b>credit:</b> Created by <b>vampi62</b>. Visit <a href='https://github.com/vampi62/electrostore'>Github Project</a> for more information.</b> ";
  response += "</div>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("rootPageSend");
}