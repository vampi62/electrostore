void handleMenuOTA(AsyncWebServerRequest *request)
{
  Serial.println("otaPageLoading");
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
  response += "<title>Paramètres User</title>";
  response += "<style>";
  response += "  body { font-family: Arial, sans-serif; background-color: #f4f4f9; color: #333; margin: 0; padding: 20px; }";
  response += "  h1 { text-align: center; color: #4CAF50; }";
  response += "  #notification { max-width: 400px; display: none; padding: 10px; margin: 15px auto; border-radius: 5px; font-size: 14px; }";
  response += "  .form-container { max-width: 400px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); }";
  response += "  button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }";
  response += "  button:hover { background-color: #45a049; }";
  response += "  a { display: block; text-align: center; margin: 15px; text-decoration: none; color: #4CAF50; }";
  response += "  a:hover { text-decoration: underline; }";
  response += "</style>";
  response += "<script>";
  response += "function enableOTA() {";
  response += "  fetch('/saveota')";
  response += "    .then(response => {";
  response += "      if (response.status === 204) {";
  response += "        document.getElementById('notification').innerText = 'OTA started successfully!';";
  response += "        document.getElementById('notification').style.color = 'white';";
  response += "        document.getElementById('notification').style.backgroundColor = 'green';";
  response += "        document.getElementById('notification').style.display = 'block';";
  response += "      } else {";
  response += "        document.getElementById('notification').innerText = 'Error: ' + response.statusText;";
  response += "        document.getElementById('notification').style.color = 'white';";
  response += "        document.getElementById('notification').style.backgroundColor = 'red';";
  response += "        document.getElementById('notification').style.display = 'block';";
  response += "      }";
  response += "    })";
  response += "}";
  response += "setInterval(function() {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('otaWait').innerText = data.OTAWait;";
  response += "    document.getElementById('otaUploading').innerText = data.OTAUploading;";
  response += "    document.getElementById('otaPercentage').innerText = data.OTAPercentage + '%';";
  response += "    document.getElementById('otaError').innerText = data.OTAError;";
  response += "    document.getElementById('otaTime').innerText = data.OTATime + 's';";
  response += "    document.getElementById('otaRemainingTime').innerText = data.OTARemainingTime + 's';";
  response += "  });";
  response += "}, 5000);";
  response += "</script>";
  response += "</head>";
  response += "<body>";
  response += "<h1>OTA Settings</h1>";
  response += "<div id='notification'></div>";
  response += "<div class='form-container'>";
  response += "<div class='ota-info'>";
  response += "  <p>Wait: <span id='otaWait'>" + String(waitingOTA ? "Waiting" : "Not Waiting") + "</span></p>";
  response += "  <p>Uploading: <span id='otaUploading'>" + String(updateOTA ? "Uploading" : "Not Uploading") + "</span></p>";
  response += "  <p>Percentage: <span id='otaPercentage'>" + String(int(otaPercentage)) + "%</span></p>";
  response += "  <p>ErrorMSG: <span id='otaError'>" + String(updateOTAError) + "</span></p>";
  response += "  <p>Time: <span id='otaTime'>" + String(IntervalOTA / 1000) + "s</span></p>";
  response += "  <p>Remaining Time: <span id='otaRemainingTime'>" + String(waitingOTA && (((IntervalOTA - (millis() - startTimeOTA)) / 1000) > 0) ? (IntervalOTA - (millis() - startTimeOTA)) / 1000 : 0) + "s</span></p>";
  response += "</div>";
  response += "<button id='enableOTAButton' onclick='enableOTA()'>Enable OTA</button>";
  response += "</div>";
  response += "<a href='/'>Back</a>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("otaPageSend");
}

void handleOTA(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  startTimeOTA = millis();
  waitingOTA = true;
  updateOTAError = "";
  request->send(204, "text/plain", ""); // No content response
}