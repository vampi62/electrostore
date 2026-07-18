#ifndef OTA_MANAGER_H
#define OTA_MANAGER_H

#include <Arduino.h>
#include <ArduinoOTA.h>

class OTAManager {
private:
    static OTAManager* _instance;

    bool          _updating;
    uint8_t       _progress;
    String        _lastError;
    bool          _windowOpen;
    unsigned long _windowStart;
    unsigned long _windowDuration;
public:
    explicit OTAManager();

    // Initialise et démarre le service OTA
    void begin();

    void setPassword(const String& password);

    // À appeler dans loop() — traite les requêtes OTA si la fenêtre est ouverte
    void handle();

    // Ouvre une fenêtre de mise à jour pour une durée donnée (ms)
    void openWindow(unsigned long durationMs);

    bool isWindowOpen() const;
    unsigned long getRemainingTime() const;
    bool isUpdating() const;
    uint8_t getProgress() const;
    String getLastError() const;
};

#endif
