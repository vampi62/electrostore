void handleMenuWifi(AsyncWebServerRequest *request)
{
  Serial.println("wifiPageLoading");
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  // si wifiSSID contient "�" alors wifiSSID = ""
  String response = "<!DOCTYPE html>";
  response += "<html lang='fr'>";
  response += "<head>";
  response += "<meta charset='UTF-8'>";
  response += "<meta name='viewport' content='width=device-width, initial-scale=1.0'>";
  response += "<title>WiFi Settings</title>";
  response += "<style>";
  response += "  body { font-family: Arial, sans-serif; background-color: #f4f4f9; color: #333; margin: 0; padding: 20px; }";
  response += "  h1 { text-align: center; color: #4CAF50; }";
  response += "  #notification { max-width: 400px; display: none; padding: 10px; margin: 15px auto; border-radius: 5px; font-size: 14px; }";
  response += "  .form-container { max-width: 400px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); }";
  response += "  .form-group { margin-bottom: 15px; }";
  response += "  label { display: block; margin-bottom: 5px; font-weight: bold; }";
  response += "  input[type='text'], input[type='password'] { width: calc(100% - 10px); padding: 8px; border: 1px solid #ccc; border-radius: 4px; }";
  response += "  input[type='checkbox'] { margin-right: 5px; }";
  response += "  button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }";
  response += "  button:hover { background-color: #45a049; }";
  response += "  .info { max-width: 400px; margin: 0 auto; background: #fff; padding: 20px; border-radius: 8px; box-shadow: 0 4px 8px rgba(0, 0, 0, 0.1); }";
  response += "  .info b { color: #4CAF50; }";
  response += "  a { display: block; text-align: center; margin: 15px; text-decoration: none; color: #4CAF50; }";
  response += "  a:hover { text-decoration: underline; }";
  response += "</style>";
  response += "<script>";
  response += "function saveWifiSettings() {";
  response += "  const ssid = document.getElementById('wifiSSID').value;";
  response += "  const password = document.getElementById('wifiPassword').value;";
  response += "  const togglePassword = document.getElementById('toggleWifiPassword').checked ? 'on' : 'off';";
  response += "  const notification = document.getElementById('notification');";
  response += "  fetch('/savewifi?wifiSSID=' + encodeURIComponent(ssid) + '&wifiPassword=' + encodeURIComponent(password) + '&checkWifiPassword=' + encodeURIComponent(togglePassword))";
  response += "    .then(response => response.json())";
  response += "    .then(data => {";
  response += "      notification.innerText = data.status === 'Success' ? 'Settings saved successfully !' : 'Error : ' + data.msg;";
  response += "      notification.style.color = 'white';";
  response += "      notification.style.backgroundColor = data.status === 'Success' ? 'green' : 'red';";
  response += "      notification.style.display = 'block';";
  response += "    })";
  response += "    .catch(error => {";
  response += "      notification.innerText = 'Error : ' + error.message;";
  response += "      notification.style.color = 'white';";
  response += "      notification.style.backgroundColor = 'red';";
  response += "      notification.style.display = 'block';";
  response += "    });";
  response += "}";
  response += "function toggleWifiPasswordField() {";
  response += "  const passwordField = document.getElementById('wifiPassword');";
  response += "  passwordField.disabled = !document.getElementById('toggleWifiPassword').checked;";
  response += "  passwordField.value = '';";
  response += "}";
  response += "setInterval(() => {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('wifiSignalStrength').innerText = data.wifiSignalStrength + ' dBm';";
  response += "  });";
  response += "}, 5000);";
  response += "</script>";
  response += "</head>";
  response += "<body>";
  response += "<h1>WiFi Settings</h1>";
  response += "<div id='notification'></div>";
  response += "<div class='form-container'>";
  response += "  <div class='form-group'>";
  response += "    <label for='wifiSSID'>SSID:</label>";
  response += "    <input type='text' id='wifiSSID' value='" + wifiSSID + "' maxlength='50'>";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <input type='checkbox' id='toggleWifiPassword' onchange='toggleWifiPasswordField()' " + String((wifiPassword.length() > 0 ? "checked" : "")) + "> Password required";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='wifiPassword'>Password:</label>";
  response += "    <input type='password' id='wifiPassword' maxlength='50' " + String((wifiPassword.length() > 0 ? "placeholder='********'" : "disabled")) + ">";
  response += "  </div>";
  response += "  <button onclick='saveWifiSettings()'>Save</button>";
  response += "</div>";
  response += "<a href='/'>Back</a>";
  response += "<div class='info'>";
  response += "  <b>IP Address:</b> " + (WiFi.getMode() != WIFI_AP ? WiFi.localIP().toString() : WiFi.softAPIP().toString()) + "<br>";
  response += "  <b>MAC Address:</b> " + WiFi.macAddress() + "<br>";
  response += "  <b>SSID:</b> " + WiFi.SSID() + "<br>";
  response += "  <b>Signal Strength:</b> <span id='wifiSignalStrength'>" + String(WiFi.RSSI()) + " dBm</span><br>";
  response += "  <b>WiFi Mode:</b> " + String((WiFi.getMode() != WIFI_AP ? "Station" : "Access Point")) + "<br>";
  response += "</div>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("wifiPageLoading");
}

void handleSaveWifi(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String newWIFISSID = request->arg("wifiSSID");
  String newWifiPassword = request->arg("wifiPassword");
  String newCheckWifiPassword = request->arg("checkWifiPassword");
  bool FormChange = false;

  if (newWIFISSID.length() > 50 || newWifiPassword.length() > 50 || newCheckWifiPassword.length() > 50) {
    StaticJsonDocument<100> doc;
    doc["msg"] = "Input exceeds maximum length of 50 characters.";
    doc["status"] = "Error";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(400, "application/json", jsonResponse);
    return;
  }

  if (newWIFISSID.length() > 0)
  {
    if (wifiSSID != newWIFISSID)
    {
      wifiSSID = newWIFISSID;
      writeStringToEEPROM(WIFISSID_ADDRESS, wifiSSID);
      FormChange = true;
    }
  }
  if (newCheckWifiPassword != NULL)
  {
    if (newCheckWifiPassword == "on")
    {
      if (newWifiPassword.length() > 0)
      {
        if (wifiPassword != newWifiPassword)
        {
          wifiPassword = newWifiPassword;
          writeStringToEEPROM(WIFIPASSWORD_ADDRESS, wifiPassword);
          FormChange = true;
        }
      }
    }
    else
    {
      if (wifiPassword != "")
      {
        wifiPassword = "";
        writeStringToEEPROM(WIFIPASSWORD_ADDRESS, wifiPassword);
        FormChange = true;
      }
    }
  }
  else
  {
    if (newWifiPassword != NULL)
    {
      if (newWifiPassword.length() > 0 && wifiPassword != newWifiPassword) // no useless writing
      {
        wifiPassword = newWifiPassword;
        writeStringToEEPROM(WIFIPASSWORD_ADDRESS, wifiPassword);
        FormChange = true;
      }
    }
  }
  
  if (FormChange)
  {
    StaticJsonDocument<100> doc;
    doc["msg"] = "change saved, Module restarting.";
    doc["status"] = "Success";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(200, "application/json", jsonResponse);
    ESP.restart();
  }
  else
  {
    StaticJsonDocument<100> doc;
    doc["msg"] = "No change saved.";
    doc["status"] = "Error";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(400, "application/json", jsonResponse);
  }
}