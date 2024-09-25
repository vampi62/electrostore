void handleMenuWifi() {
  Serial.println("wifiPageLoading");
  // si ssid contient "�" alors ssid = ""
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Paramètres WiFi</h1>";
  response += "<form action='/savewifi' method='get'>";
  response += "SSID: <input type='text' name='ssid' value='" + ssid + "'><br>";
  if (password.length() > 0) {
    response += "Mot de passe: <input type='password' name='password' placeholder='********'><br>";
  } else {
    response += "Mot de passe: <input type='password' name='password'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  server.send(200, "text/html", response);
  Serial.println("wifiPageLoading");
}

void handleSaveWifi() {
  String newSSID = server.arg("ssid");
  String newPassword = server.arg("password");
  bool FormChange = false;
  if (newSSID.length() > 0) {
    if (ssid != newSSID){
      ssid = newSSID;
      writeStringToEEPROM(SSID_ADDRESS, ssid);
      FormChange = true;
    }
  }
  if (newPassword.length() > 0) {
    if (password != newPassword){
      password = newPassword;
      writeStringToEEPROM(PASSWORD_ADDRESS, password);
      FormChange = true;
    }
  }
  if (FormChange) {
    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
    ESP.restart();
  } else {
    server.send(400, "text/plain", "Erreur: pas de changement.");
  }
}

void handleMenuCam() {
  Serial.println("camPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Paramètres CAM</h1>";
  response += "<form action='/savecam' method='get'>";
  response += "CAM User: <input type='text' name='camuser' value='" + camUser + "'><br>";
  if (camPassword.length() > 0) {
    response += "CAM Password: <input type='password' name='campassword' placeholder='********'><br>";
  } else {
    response += "CAM Password: <input type='password' name='campassword'><br>";
  }
  response += "<input type='submit' value='Enregistrer'>";
  response += "</form>";
  response += "<a href='/'>Retour</a>";
  response += "</body></html>";
  server.send(200, "text/html", response);
  Serial.println("camPageSend");
}

void handleSaveCam() {
  String newCamUser = server.arg("camuser");
  String newCamPassword = server.arg("campassword");
  bool FormChange = false;
  if (newCamUser.length() > 0) {
    if (camUser != newCamUser){
      camUser = newCamUser;
      writeStringToEEPROM(CAMUSER_ADDRESS, camUser);
      FormChange = true;
    }
  }
  if (newCamPassword.length() > 0) {
    if (camPassword != newCamPassword){
      camPassword = newCamPassword;
      writeStringToEEPROM(CAMPASSWORD_ADDRESS, camPassword);
      FormChange = true;
    }
  }
  if (FormChange) {
    server.send(200, "text/plain", "Paramètres enregistrés. Redémarrage du module.");
  } else {
    server.send(400, "text/plain", "Erreur: pas de changement.");
  }
}

void handleLight() {
  // si un user et un mot de passe sont définis
  if (camUser.length() > 0 && camPassword.length() > 0) {
    // vérification de l'authentification
    if (!server.authenticate(camUser.c_str(), camPassword.c_str())) {
      return server.requestAuthentication();
    }
  }
  String lightState = server.arg("state");
  if (lightState == "on") {
    for (int i = 1; i < 64; i++) {
      strip.setPixelColor(i, strip.Color(50, 50, 50));
    }
  } else {
    for (int i = 1; i < 64; i++) {
      strip.setPixelColor(i, strip.Color(0, 0, 0));
    }
  }
  strip.show();
}

void stream_handler() {
  // Configurer les en-têtes HTTP pour indiquer un flux MJPEG
  if (!server.client()) {
    return;
  }

  // si un user et un mot de passe sont définis
  if (camUser.length() > 0 && camPassword.length() > 0) {
    // vérification de l'authentification
    if (!server.authenticate(camUser.c_str(), camPassword.c_str())) {
      return server.requestAuthentication();
    }
  }


  // Envoyer l'en-tête HTTP pour indiquer un flux MJPEG
  String response = "HTTP/1.1 200 OK\r\n";
  response += "Content-Type: multipart/x-mixed-replace; boundary=123456789000000000000987654321\r\n";
  response += "Connection: close\r\n";
  response += "\r\n";
  server.client().print(response);

  // Boucle pour envoyer des images en continu
  while (true) {
    // Capturer une image à partir de la caméra
    camera_fb_t *fb = esp_camera_fb_get();
    if (!fb) {
      Serial.println("Échec de la capture d'image");
      break;
    }

    // Envoyer les données MJPEG (image) au client
    size_t jpg_len = fb->len;
    if (fb->format != PIXFORMAT_JPEG) {
      Serial.println("Format non supporté !");
      esp_camera_fb_return(fb);
      break;
    }

    // Envoyer la partie du flux MJPEG
    server.client().printf("\r\n--123456789000000000000987654321\r\n");
    server.client().printf("Content-Type: image/jpeg\r\nContent-Length: %u\r\n\r\n", jpg_len);
    server.client().write(fb->buf, fb->len);

    // Libérer le buffer de la caméra
    esp_camera_fb_return(fb);

    // Si le client a arrêté la connexion, quitter la boucle
    if (!server.client().connected()) {
      break;
    }

    // Petite pause pour éviter une surcharge
    delay(50);
  }
}



void handleRoot() {
  Serial.println("rootPageLoading");
  String response = "<html><head><meta charset='UTF-8'></head><body>";
  response += "<h1>Menu</h1>";
  response += "<ul>";
  response += "<li><a href='/menuwifi'>Paramètres WiFi</a></li>";
  response += "<li><a href='/menucam'>Paramètres CAM</a></li>";
  response += "</ul>";
  response += "</body></html>";
  server.send(200, "text/html", response);
  Serial.println("rootPageSend");
}