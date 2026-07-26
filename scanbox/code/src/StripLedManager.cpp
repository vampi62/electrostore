#include "StripLedManager.h"

StripLedManager::StripLedManager() {}

void StripLedManager::begin() {
    for (int i = 0; i < LED_COUNT+1; i++) {
        leds[i] = new LEDInfo{0, 0, 0, 0, 0};
    }
    strip = new Adafruit_NeoPixel(LED_COUNT+1, LED_PIN, NEO_GRB + NEO_KHZ800);
    strip->begin();
    strip->show(); // Initialize all pixels to 'off'
    startTimeLed = millis();
}

void StripLedManager::setLed(int index, uint8_t red, uint8_t green, uint8_t blue, uint8_t module, uint32_t delayTime) {
    if (index < 0 || index >= LED_COUNT+1) {
        return; // Index out of bounds
    }
    leds[index]->red = red;
    leds[index]->green = green;
    leds[index]->blue = blue;
    leds[index]->module = module;
    leds[index]->delayTime = delayTime;
    strip->setPixelColor(index, strip->Color(red, green, blue));
    strip->show();
}

void StripLedManager::setRingLight(bool on) {
    ringLightOn = on;
    uint8_t level = on ? 255 : 0;
    uint32_t delayTime = on ? 0x7FFFFFFF : 0; // ~24 days: stays on until explicitly turned off
    for (int i = HAS_LED_IN_BOX ? 0 : 1; i < LED_COUNT+1; i++) {
        setLed(i, level, level, level, 1, delayTime);
    }
}

void StripLedManager::calculateAnimationMode() {
    inputLoop = inputLoop + 0.01;
    if (inputLoop >= 1080)
    {
      inputLoop = 0;
    }
    modSlowSin = fabs(sin(inputLoop));
    modModerateSin = fabs(sin(inputLoop / 0.5));
    modQuickSin = fabs(sin(inputLoop / 0.25));
    modFastSin = fabs(sin(inputLoop / 0.125));
}

void StripLedManager::calculateDelayTime() {
    for (int i = 0; i < LED_COUNT+1; i++) {
        if (leds[i]->delayTime > 0) {
            leds[i]->delayTime -= (millis() - startTimeLed);
        }
    }
    startTimeLed = millis();
}

void StripLedManager::show() {
    calculateAnimationMode();
    calculateDelayTime();
    strip->clear();
    for (int i = 0; i < LED_COUNT+1; i++) {
        if (leds[i]->delayTime > 0)
        {
            switch (leds[i]->module)
            {
                case 1:
                    strip->setPixelColor(i, strip->Color(leds[i]->red, leds[i]->green, leds[i]->blue));
                    break;
                case 2:
                    strip->setPixelColor(i, strip->Color(leds[i]->red * modSlowSin, leds[i]->green * modSlowSin, leds[i]->blue * modSlowSin));
                    break;
                case 3:
                    strip->setPixelColor(i, strip->Color(leds[i]->red * modModerateSin, leds[i]->green * modModerateSin, leds[i]->blue * modModerateSin));
                    break;
                case 4:
                    strip->setPixelColor(i, strip->Color(leds[i]->red * modQuickSin, leds[i]->green * modQuickSin, leds[i]->blue * modQuickSin));
                    break;
                case 5:
                    strip->setPixelColor(i, strip->Color(leds[i]->red * modFastSin, leds[i]->green * modFastSin, leds[i]->blue * modFastSin));
                    break;
                default:
                    strip->setPixelColor(i, strip->Color(leds[i]->red, leds[i]->green, leds[i]->blue));
                    break;
            }
        }
    }
    strip->show();
}