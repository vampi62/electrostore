#include <ESP8266WiFi.h>
#include <ESP8266WebServer.h>

const char *ssid = "MonReseauWiFi";  // Nom du réseau WiFi souhaité
const char *password = "MotDePasse"; // Mot de passe du réseau WiFi souhaité

const char *ap_ssid = "ESP_Config"; // Nom du réseau WiFi en mode AP (point d'accès)
const char *ap_password = "ConfigPass"; // Mot de passe du réseau WiFi en mode AP

ESP8266WebServer server(80);

void setup() {
  Serial.begin(115200);
  delay(100);

  WiFi.mode(WIFI_STA);
  WiFi.begin(ssid, password);

  Serial.println("Tentative de connexion au WiFi...");

  int attempt = 0;
  while (WiFi.status() != WL_CONNECTED && attempt < 10) {
    delay(1000);
    Serial.println("Connexion en cours...");
    attempt++;
  }

  if (WiFi.status() == WL_CONNECTED) {
    Serial.println("Connecté au réseau WiFi");
    Serial.print("Adresse IP: ");
    Serial.println(WiFi.localIP());
  } else {
    Serial.println("Impossible de se connecter au WiFi");
    startAPMode();
  }

  server.on("/", HTTP_GET, handleRoot);
  server.begin();
}

void loop() {
  server.handleClient();
}

void startAPMode() {
  WiFi.mode(WIFI_AP);
  WiFi.softAP(ap_ssid, ap_password);

  Serial.println("Mode point d'accès activé");
  Serial.print("Adresse IP en mode AP: ");
  Serial.println(WiFi.softAPIP());
}

void handleRoot() {
  String html = "<html><body>";
  html += "<h1>Configuration ESP</h1>";
  html += "<form action='/setwifi' method='POST'>";
  html += "SSID: <input type='text' name='ssid'><br>";
  html += "Password: <input type='password' name='password'><br>";
  html += "<input type='submit' value='Configurer'>";
  html += "</form>";
  html += "</body></html>";
  server.send(200, "text/html", html);
}

void handleSetWiFi() {
  String newSSID = server.arg("ssid");
  String newPassword = server.arg("password");

  if (newSSID.length() > 0 && newPassword.length() > 0) {
    WiFi.begin(newSSID, newPassword);
    int attempt = 0;
    while (WiFi.status() != WL_CONNECTED && attempt < 10) {
      delay(1000);
      Serial.println("Tentative de connexion au WiFi...");
      attempt++;
    }

    if (WiFi.status() == WL_CONNECTED) {
      Serial.println("Connecté au réseau WiFi");
      Serial.print("Adresse IP: ");
      Serial.println(WiFi.localIP());
      server.send(200, "text/plain", "Connecté au réseau WiFi");
    } else {
      Serial.println("Impossible de se connecter au WiFi");
      server.send(200, "text/plain", "Échec de la connexion au WiFi");
    }
  } else {
    server.send(400, "text/plain", "Paramètres invalides");
  }
}
