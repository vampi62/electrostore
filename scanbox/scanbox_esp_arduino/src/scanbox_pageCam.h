void handleMenuCam(AsyncWebServerRequest *request)
{
  Serial.println("camPageLoading");
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String response = "<!DOCTYPE html>";
  response += "<html lang='fr'>";
  response += "<head>";
  response += "<meta charset='UTF-8'>";
  response += "<meta name='viewport' content='width=device-width, initial-scale=1.0'>";
  response += "<title>Paramètres CAM</title>";
  response += "<style>";
  response += "  body { font-family: Arial, sans-serif; background-color: #f4f4f9; color: #333; margin: 0; padding: 20px; }";
  response += "  h1 { text-align: center; color: #4CAF50; }";
  response += "  .info { max-width: 640px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); }";
  response += "  .info b { color: #4CAF50; }";
  response += "  a { display: block; text-align: center; margin: 15px; text-decoration: none; color: #4CAF50; }";
  response += "  a:hover { text-decoration: underline; }";
  response += "  img { display: block; margin: 0 auto; }";
  response += "  label { display: block; margin-top: 15px; font-weight: bold; }";
  response += "  input[type='checkbox'], input[type='range'] { margin-top: 5px; }";
  response += "</style>";
  response += "<script>";
  response += "setInterval(function() {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('espTemperature').innerText = data.espTemperature + '°C';";
  response += "    document.getElementById('uptime').innerText = data.uptime + 's';";
  response += "  });";
  response += "}, 5000);";
  response += "let lastValue = " + String(ringLightPower) + ";";
  response += "document.addEventListener('DOMContentLoaded', function() {";
  response += "  document.getElementById('lightSwitch').checked = " + String((ringLightPower > 0 ? "true" : "false")) + ";";
  response += "  document.getElementById('ringLightPower').style.display = " + String((ringLightPower > 0 ? "'block'" : "'none'")) + ";";
  response += "});";
  response += "function toggleLight() {";
  response += "  const rangeInput = document.getElementById('ringLightPower');";
  response += "  const lightSwitch = document.getElementById('lightSwitch');";
  response += "  if (lightSwitch.checked) {";
  response += "    rangeInput.style.display = 'block';";
  response += "    updateLight(lastValue);";
  response += "  } else {";
  response += "    lastValue = rangeInput.value;";
  response += "    rangeInput.style.display = 'none';";
  response += "    updateLight(0);";
  response += "  }";
  response += "}";
  response += "function updateLight(val) {";
  response += "  let xhr = new XMLHttpRequest();";
  response += "  xhr.open('GET', '/light?ringLightPower=' + val, true);";
  response += "  xhr.send();";
  response += "}";
  response += "</script>";
  response += "</head>";
  response += "<body>";
  response += "<h1>CAM Settings</h1>";
  response += "<a href='/'>Back</a>";
  response += "<div class='info'>";
  response += "<img src='/stream' width='640' height='480'><br>";
  response += "<b>ScanBox Version:</b> " + String(version_scanbox) + "<br>";
  response += "<b>Camera Resolution:</b> " + camResolution + "<br>";
  sensor_t *s = esp_camera_sensor_get();
  response += "<b>Camera PID:</b> " + String(s->id.PID) + "<br>";
  response += "<b>ESP Temperature:</b> <span id='espTemperature'>" + String(temperatureRead()) + "°C</span><br>";
  response += "<b>ESP Model:</b> " + String(ESP.getChipModel()) + "<br>";
  response += "<b>ESP Uptime:</b> <span id='uptime'>" + String(millis() / 1000) + "s</span><br>";
  response += "<label for='lightSwitch'>Ring Light:</label> <input type='checkbox' id='lightSwitch' onchange='toggleLight()'><br>";
  response += "<input type='range' id='ringLightPower' name='ringLightPower' min='0' max='255' value='" + String(ringLightPower) + "' onchange='updateLight(this.value)'>";
  response += "</div>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("camPageSend");
}