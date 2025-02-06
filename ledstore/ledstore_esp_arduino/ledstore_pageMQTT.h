void handleMenuMqtt(AsyncWebServerRequest *request)
{
  Serial.println("mqttPageLoading");
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
  response += "<title>Mqtt Settings</title>";
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
  response += "function saveMqttSettings() {";
  response += "  const mqttServer = document.getElementById('mqttServer').value;";
  response += "  const mqttPort = document.getElementById('mqttPort').value;";
  response += "  const mqttName = document.getElementById('mqttName').value;";
  response += "  const mqttUser = document.getElementById('mqttUser').value;";
  response += "  const mqttPassword = document.getElementById('mqttPassword').value;";
  response += "  const mqttTopic = document.getElementById('mqttTopic').value;";
  response += "  const toggleMQTTPassword = document.getElementById('toggleMqttPassword').checked ? 'on' : 'off';";
  response += "  const notification = document.getElementById('notification');";
  response += "  fetch('/savemqtt?mqttServer=' + encodeURIComponent(mqttServer) + '&mqttPort=' + encodeURIComponent(mqttPort) + '&mqttName=' + encodeURIComponent(mqttName) + '&mqttUser=' + encodeURIComponent(mqttUser) + '&mqttPassword=' + encodeURIComponent(mqttPassword) + '&mqttTopic=' + encodeURIComponent(mqttTopic) + '&checkMQTTPassword=' + encodeURIComponent(toggleMQTTPassword))";
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
  response += "function toggleMqttPasswordField() {";
  response += "  const passwordField = document.getElementById('mqttPassword');";
  response += "  passwordField.disabled = !document.getElementById('toggleMqttPassword').checked;";
  response += "  passwordField.value = '';";
  response += "}";
  response += "setInterval(() => {";
  response += "  fetch('/status').then(response => response.json()).then(data => {";
  response += "    document.getElementById('uptime').innerText = data.uptime + ' s';";
  response += "  });";
  response += "}, 5000);";
  response += "</script>";
  response += "</head>";
  response += "<body>";
  response += "<h1>Mqtt Settings</h1>";
  response += "<div id='notification'></div>";
  response += "<div class='form-container'>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttServer'>SERVER:</label>";
  response += "    <input type='text' id='mqttServer' value='" + mqttServer + "' maxlength='50'>";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttPort'>PORT:</label>";
  response += "    <input type='text' id='mqttPort' value='" + mqttPort + "' maxlength='50'>";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttName'>NAME:</label>";
  response += "    <input type='text' id='mqttName' value='" + mqttName + "' maxlength='50'>";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttUser'>Username:</label>";
  response += "    <input type='text' id='mqttUser' value='" + mqttUser + "' maxlength='50'>";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <input type='checkbox' id='toggleMqttPassword' onchange='toggleMqttPasswordField()' " + String((mqttPassword.length() > 0 ? "checked" : "")) + "> Password required";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttPassword'>Password:</label>";
  response += "    <input type='password' id='mqttPassword' maxlength='50' " + String((mqttPassword.length() > 0 ? "placeholder='********'" : "disabled")) + ">";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='mqttTopic'>Topic:</label>";
  response += "    <input type='text' id='mqttTopic' value='" + mqttTopic + "' maxlength='50'>";
  response += "  </div>";
  response += "  <button onclick='saveMqttSettings()'>Save</button>";
  response += "</div>";
  response += "<a href='/'>Back</a>";
  response += "<div class='info'>";
  response += "<b>ScanBox Version:</b> " + String(version_ledstore) + "<br>";
  response += "<b>ESP Model:</b> " + String(ESP.getChipId()) + "<br>";
  response += "<b>ESP Uptime:</b> <span id='uptime'>" + String(millis() / 1000) + "s</span><br>";
  response += "</div>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("mqttPageLoading");
}

void handleSaveMqtt(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String newMqttServer = request->arg("mqttServer");
  String newMqttPort = request->arg("mqttPort");
  String newMqttName = request->arg("mqttName");
  String newMqttTopic = request->arg("mqttTopic");
  String newMqttUser = request->arg("mqttUser");
  String newMqttPassword = request->arg("mqttPassword");
  String newCheckMQTTPassword = request->arg("checkMQTTPassword");
  bool FormChange = false;

  if (newMqttServer.length() > 50 || newMqttPort.length() > 50 || newMqttName.length() > 50 || newMqttUser.length() > 50 || newMqttPassword.length() > 50 || newMqttTopic.length() > 50 || newCheckMQTTPassword.length() > 50) {
    StaticJsonDocument<100> doc;
    doc["msg"] = "Input exceeds maximum length of 50 characters.";
    doc["status"] = "Error";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(400, "application/json", jsonResponse);
    return;
  }

  if (newMqttServer.length() > 0)
  {
    if (mqttServer != newMqttServer) // no useless writing
    {
      mqttServer = newMqttServer;
      writeStringToEEPROM(MQTTSERVER_ADDRESS, mqttServer);
      FormChange = true;
    }
  }
  if (newMqttPort.length() > 0)
  {
    if (mqttPort != newMqttPort) // no useless writing
    {
      mqttPort = newMqttPort;
      writeStringToEEPROM(MQTTPORT_ADDRESS, mqttPort);
      FormChange = true;
    }
  }
  if (newMqttName.length() > 0)
  {
    if (mqttName != newMqttName) // no useless writing
    {
      mqttName = newMqttName;
      writeStringToEEPROM(MQTTNAME_ADDRESS, mqttName);
      FormChange = true;
    }
  }
  if (newMqttTopic.length() > 0)
  {
    if (mqttTopic != newMqttTopic) // no useless writing
    {
      mqttTopic = newMqttTopic;
      writeStringToEEPROM(MQTTTOPIC_ADDRESS, mqttTopic);
      FormChange = true;
    }
  }

  if (newMqttUser != NULL)
  {
    if (mqttUser != newMqttUser) // no useless writing
    {
      mqttUser = newMqttUser;
      writeStringToEEPROM(MQTTUSER_ADDRESS, mqttUser);
      FormChange = true;
    }
  }
  if (newCheckMQTTPassword != NULL)
  {
    if (newCheckMQTTPassword == "on")
    {
      if (newMqttPassword.length() > 0)
      {
        if (mqttPassword != newMqttPassword) // no useless writing
        {
          mqttPassword = newMqttPassword;
          writeStringToEEPROM(MQTTPASSWORD_ADDRESS, mqttPassword);
          FormChange = true;
        }
      }
    }
    else
    {
      if (mqttPassword != "") // no useless writing
      {
        mqttPassword = "";
        writeStringToEEPROM(MQTTPASSWORD_ADDRESS, mqttPassword);
        FormChange = true;
      }
    }
  }
  else
  {
    if (newMqttPassword != NULL)
    {
      if (newMqttPassword.length() > 0 && mqttPassword != newMqttPassword) // no useless writing
      {
        mqttPassword = newMqttPassword;
        writeStringToEEPROM(MQTTPASSWORD_ADDRESS, mqttPassword);
        FormChange = true;
      }
    }
  }

  if (FormChange)
  {
    StaticJsonDocument<100> doc;
    doc["msg"] = "change saved.";
    doc["status"] = "Success";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(200, "application/json", jsonResponse);
    nbrErreurMqttConnect = 0;
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