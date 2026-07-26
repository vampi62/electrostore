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

    // Initializes and starts the OTA service
    void begin();

    void setPassword(const String& password);

    // Call in loop() — processes OTA requests if the window is open
    void handle();

    // Opens an update window for a given duration (ms)
    void openWindow(unsigned long durationMs);

    bool isWindowOpen() const { return _windowOpen; }
    unsigned long getRemainingTime() const;
    bool isUpdating() const { return _updating; }
    uint8_t getProgress() const { return _progress; }
    String getLastError() const { return _lastError; }
};

#endif
