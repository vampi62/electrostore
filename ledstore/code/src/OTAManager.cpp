#include "config.h"
#include "OTAManager.h"
#include "Logger.h"

OTAManager::OTAManager() {}

void OTAManager::begin() {
    ArduinoOTA.setHostname(OTA_HOSTNAME);
    ArduinoOTA.setPort(OTA_PORT);

    setPassword(OTA_DEFAULT_PASSWORD);

    ArduinoOTA.onStart([this]() {
        if (this) {
            this->_updating  = true;
            this->_progress  = 0;
            this->_lastError = "";
        }
        Logger::info("OTA: update starting");
    });

    ArduinoOTA.onEnd([this]() {
        if (this) {
            this->_updating = false;
        }
        Logger::info("OTA: update complete");
    });

    ArduinoOTA.onProgress([this](unsigned int progress, unsigned int total) {
        if (this) {
            this->_progress = (uint8_t)(progress / (total / 100));
        }
        Serial.printf("OTA: %u%%\r", progress / (total / 100));
    });

    ArduinoOTA.onError([this](ota_error_t error) {
        if (this) {
            this->_updating  = false;
            this->_lastError = String(error);
        }
        String msg = "OTA error [" + String(error) + "]";
        if      (error == OTA_AUTH_ERROR)    msg += " : Auth Failed";
        else if (error == OTA_BEGIN_ERROR)   msg += " : Begin Failed";
        else if (error == OTA_CONNECT_ERROR) msg += " : Connect Failed";
        else if (error == OTA_RECEIVE_ERROR) msg += " : Receive Failed";
        else if (error == OTA_END_ERROR)     msg += " : End Failed";
        Logger::error(msg);
    });

    ArduinoOTA.begin();
    Logger::info("OTA ready — hostname: " + String(OTA_HOSTNAME) + ", port: " + String(OTA_PORT));
}

void OTAManager::setPassword(const String& password) {
    ArduinoOTA.setPassword(password.c_str());
    Logger::info("OTA: password set");
}

void OTAManager::handle() {
    if (!_windowOpen) return;

    if (!_updating && (millis() - _windowStart >= _windowDuration)) {
        _windowOpen = false;
        Logger::info("OTA: update window closed");
        return;
    }

    ArduinoOTA.handle();
}

void OTAManager::openWindow(unsigned long durationMs) {
    _windowOpen     = true;
    _windowStart    = millis();
    _windowDuration = durationMs;
    Logger::info("OTA: window opened for " + String(durationMs / 1000) + "s");
}

unsigned long OTAManager::getRemainingTime() const {
    if (!_windowOpen) return 0;
    unsigned long elapsed = millis() - _windowStart;
    return (elapsed >= _windowDuration) ? 0 : (_windowDuration - elapsed) / 1000;
}
