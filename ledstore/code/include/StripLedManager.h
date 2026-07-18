#ifndef STRIP_LED_MANAGER_H
#define STRIP_LED_MANAGER_H

#include <Arduino.h>
#include <Adafruit_NeoPixel.h>
#include "config.h"

struct LEDInfo
{
    int red;
    int green;
    int blue;
    int module;
    int delayTime;
};

class StripLedManager {
private:
    static StripLedManager* _instance;
    Adafruit_NeoPixel* strip;
    float inputLoop = 0;
    float modSlowSin = 0;
    float modModerateSin = 0;
    float modQuickSin = 0;
    float modFastSin = 0;
    unsigned long startTimeLed = 0;

    void calculateAnimationMode();
    void calculateDelayTime();
public:
    explicit StripLedManager();

    LEDInfo* leds[LED_COUNT+1]; // +1 pour la LED dans le boîtier si HAS_LED_IN_BOX est défini

    void begin();
    void setLed(int index, uint8_t red, uint8_t green, uint8_t blue, uint8_t module, uint32_t delayTime);
    void show();
};

#endif