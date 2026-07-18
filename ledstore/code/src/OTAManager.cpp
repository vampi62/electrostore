#include "config.h"
#include "OTAManager.h"
#include "Logger.h"

OTAManager* OTAManager::_instance = nullptr;

OTAManager::OTAManager()
    : _updating(false), _progress(0), _lastError(""),
      _windowOpen(false), _windowStart(0), _windowDuration(0) {}

void OTAManager::begin() {
    _instance = this;
    ArduinoOTA.setHostname(OTA_HOSTNAME);
    ArduinoOTA.setPort(OTA_PORT);

    setPassword(OTA_DEFAULT_PASSWORD);

    ArduinoOTA.onStart([]() {
        if (_instance) {
            _instance->_updating  = true;
            _instance->_progress  = 0;
            _instance->_lastError = "";
        }
        Logger::info("OTA : démarrage de la mise à jour");
    });

    ArduinoOTA.onEnd([]() {
        if (_instance) {
            _instance->_updating = false;
        }
        Logger::info("OTA : mise à jour terminée");
    });

    ArduinoOTA.onProgress([](unsigned int progress, unsigned int total) {
        if (_instance) {
            _instance->_progress = (uint8_t)(progress / (total / 100));
        }
        Serial.printf("OTA : %u%%\r", progress / (total / 100));
    });

    ArduinoOTA.onError([](ota_error_t error) {
        if (_instance) {
            _instance->_updating  = false;
            _instance->_lastError = String(error);
        }
        String msg = "OTA erreur [" + String(error) + "]";
        if      (error == OTA_AUTH_ERROR)    msg += " : Auth Failed";
        else if (error == OTA_BEGIN_ERROR)   msg += " : Begin Failed";
        else if (error == OTA_CONNECT_ERROR) msg += " : Connect Failed";
        else if (error == OTA_RECEIVE_ERROR) msg += " : Receive Failed";
        else if (error == OTA_END_ERROR)     msg += " : End Failed";
        Logger::error(msg);
    });

    ArduinoOTA.begin();
    Logger::info("OTA prêt — hostname: " + String(OTA_HOSTNAME) + ", port: " + String(OTA_PORT));
}

void OTAManager::setPassword(const String& password) {
    ArduinoOTA.setPassword(password.c_str());
    Logger::info("OTA : mot de passe défini");
}

void OTAManager::handle() {
    if (!_windowOpen) return;

    if (!_updating && (millis() - _windowStart >= _windowDuration)) {
        _windowOpen = false;
        Logger::info("OTA : fenêtre de mise à jour fermée");
        return;
    }

    ArduinoOTA.handle();
}

void OTAManager::openWindow(unsigned long durationMs) {
    _windowOpen     = true;
    _windowStart    = millis();
    _windowDuration = durationMs;
    Logger::info("OTA : fenêtre ouverte pour " + String(durationMs / 1000) + "s");
}

unsigned long OTAManager::getRemainingTime() const {
    if (!_windowOpen) return 0;
    unsigned long elapsed = millis() - _windowStart;
    return (elapsed >= _windowDuration) ? 0 : (_windowDuration - elapsed) / 1000;
}

bool OTAManager::isWindowOpen() const {
    return _windowOpen;
}

bool OTAManager::isUpdating() const {
    return _updating;
}

uint8_t OTAManager::getProgress() const {
    return _progress;
}

String OTAManager::getLastError() const {
    return _lastError;
}
