void handleMenuUser(AsyncWebServerRequest *request)
{
  Serial.println("userPageLoading");
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
  response += "  .form-group { margin-bottom: 15px; }";
  response += "  label { display: block; margin-bottom: 5px; font-weight: bold; }";
  response += "  input[type='text'], input[type='password'] { width: calc(100% - 10px); padding: 8px; border: 1px solid #ccc; border-radius: 4px; }";
  response += "  input[type='checkbox'] { margin-right: 5px; }";
  response += "  button { width: 100%; padding: 10px; background-color: #4CAF50; color: white; border: none; border-radius: 4px; cursor: pointer; font-size: 16px; }";
  response += "  button:hover { background-color: #45a049; }";
  response += "  a { display: block; text-align: center; margin: 15px; text-decoration: none; color: #4CAF50; }";
  response += "  a:hover { text-decoration: underline; }";
  response += "</style>";
  response += "<script>";
  response += "function saveUserSettings() {";
  response += "  const user = document.getElementById('espUser').value;";
  response += "  const password = document.getElementById('espPassword').value;";
  response += "  const togglePassword = document.getElementById('toggleESPPassword').checked ? 'on' : 'off';";
  response += "  const notification = document.getElementById('notification');";
  response += "  fetch('/saveuser?espUser=' + encodeURIComponent(user) + '&espPassword=' + encodeURIComponent(password) + '&checkESPPassword=' + encodeURIComponent(togglePassword))";
  response += "    .then(response => response.json())";
  response += "    .then(data => {";
  response += "      notification.innerText = data.status === 'Success' ? 'Paramètres enregistrés avec succès !' : 'Erreur : ' + data.msg;";
  response += "      notification.style.color = 'white';";
  response += "      notification.style.backgroundColor = data.status === 'Success' ? 'green' : 'red';";
  response += "      notification.style.display = 'block';";
  response += "    })";
  response += "    .catch(error => {";
  response += "      notification.innerText = 'Erreur : ' + error.message;";
  response += "      notification.style.color = 'white';";
  response += "      notification.style.backgroundColor = 'red';";
  response += "      notification.style.display = 'block';";
  response += "    });";
  response += "}";
  response += "function toggleESPPasswordField() {";
  response += "  const userField = document.getElementById('espUser');";
  response += "  const passwordField = document.getElementById('espPassword');";
  response += "  const passwordConfirmField = document.getElementById('espConfirmPassword');";
  response += "  const toggleCheckbox = document.getElementById('toggleESPPassword').checked;";
  response += "  passwordField.disabled = !toggleCheckbox;";
  response += "  passwordConfirmField.disabled = !toggleCheckbox;";
  response += "  userField.disabled = !toggleCheckbox;";
  response += "  if (!toggleCheckbox) {";
  response += "    userField.value = '';";
  response += "    passwordField.value = '';";
  response += "    passwordConfirmField.value = '';";
  response += "    document.getElementById('passwordError').style.display = 'none';";
  response += "  }";
  response += "  checkFormValidity();";
  response += "}";
  response += "function checkPasswords() {";
  response += "  const password = document.getElementById('espPassword').value;";
  response += "  const confirmPassword = document.getElementById('espConfirmPassword').value;";
  response += "  const errorDiv = document.getElementById('passwordError');";
  response += "  if (password !== confirmPassword) {";
  response += "    errorDiv.style.display = 'block';";
  response += "  } else {";
  response += "    errorDiv.style.display = 'none';";
  response += "  }";
  response += "  checkFormValidity();";
  response += "}";
  response += "function checkFormValidity() {";
  response += "  const toggleCheckbox = document.getElementById('toggleESPPassword').checked;";
  response += "  const user = document.getElementById('espUser').value;";
  response += "  const password = document.getElementById('espPassword').value;";
  response += "  const confirmPassword = document.getElementById('espConfirmPassword').value;";
  response += "  const saveButton = document.getElementById('saveButton');";
  response += "  if (toggleCheckbox) {";// if password and confirm password are empty and are the placeholder set in the input field, the length is 0
  response += "    saveButton.disabled = !(user.length > 0 && password === confirmPassword && (password.placeholder !== '********' || password.length > 0));";
  response += "  } else {";
  response += "    saveButton.disabled = false;";
  response += "  }";
  response += "}";
  response += "</script>";
  response += "</head>";
  response += "<body onload='toggleESPPasswordField()'>";
  response += "<h1>Paramètres User</h1>";
  response += "<div id='notification'></div>";
  response += "<div class='form-container'>";
  response += "  <div class='form-group'>";
  response += "    <input type='checkbox' id='toggleESPPassword' onchange='toggleESPPasswordField()' " + String((espUser.length() > 0 && espPassword.length() > 0 ? "checked" : "")) + "> Activer authentification";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='espUser'>User:</label>";
  response += "    <input type='text' id='espUser' maxlength='50'" + String((espUser.length() > 0 ? "value='" + espUser + "'" : "disabled")) + ">";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='espPassword'>Mot de passe:</label>";
  response += "    <input type='password' id='espPassword' maxlength='50' oninput='checkPasswords()' " + String((espPassword.length() > 0 ? "placeholder='********'" : "disabled")) + ">";
  response += "  </div>";
  response += "  <div class='form-group'>";
  response += "    <label for='espConfirmPassword'>Confirme Mot de passe:</label>";
  response += "    <input type='password' id='espConfirmPassword' maxlength='50' oninput='checkPasswords()' " + String((espPassword.length() > 0 ? "placeholder='********'" : "disabled")) + ">";
  response += "    <div id='passwordError' style='color: red; display: none;'>Les mots de passe ne correspondent pas.</div>";
  response += "  </div>";
  response += "  <button id='saveButton' onclick='saveUserSettings()'>Enregistrer</button>";
  response += "</div>";
  response += "<a href='/'>Retour</a>";
  response += "</body>";
  response += "</html>";
  request->send(200, "text/html", response);
  Serial.println("userPageSend");
}

void handleSaveUser(AsyncWebServerRequest *request)
{
  if (WiFi.getMode() != WIFI_AP)
  {
    if (!authenticate(request))
    {
      return;
    }
  }
  String newESPUser = request->arg("espUser");
  String newESPPassword = request->arg("espPassword");
  String newCheckEspPassword = request->arg("checkESPPassword");
  bool FormChange = false;

  if (newESPUser.length() > 50 || newESPPassword.length() > 50 || newCheckEspPassword.length() > 50) {
    StaticJsonDocument<100> doc;
    doc["msg"] = "Input exceeds maximum length of 50 characters.";
    doc["status"] = "Error";
    String jsonResponse;
    serializeJson(doc, jsonResponse);
    request->send(400, "application/json", jsonResponse);
    return;
  }

  if (newCheckEspPassword != NULL)
  {
    if (newCheckEspPassword == "on")
    {
      if (newESPUser != NULL && newESPUser.length() > 0)
      {
        if (espUser != newESPUser) // no useless writing
        {
          espUser = newESPUser;
          writeStringToEEPROM(ESPUSER_ADDRESS, espUser);
          FormChange = true;
        }
      }
      if (newESPPassword != NULL && newESPPassword.length() > 0)
      {
        if (espPassword != newESPPassword) // no useless writing
        {
          espPassword = newESPPassword;
          writeStringToEEPROM(ESPPASSWORD_ADDRESS, espPassword);
          FormChange = true;
        }
      }
    }
    else
    {
      if (espUser != "") // no useless writing
      {
        espUser = "";
        writeStringToEEPROM(ESPUSER_ADDRESS, espUser);
        FormChange = true;
      }
      if (espPassword != "") // no useless writing
      {
        espPassword = "";
        writeStringToEEPROM(ESPPASSWORD_ADDRESS, espPassword);
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