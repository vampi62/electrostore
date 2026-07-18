#include "StripLedManager.h"

StripLedManager* StripLedManager::_instance = nullptr;

StripLedManager::StripLedManager() 
    : strip(nullptr),
      inputLoop(0),
      modSlowSin(0),
      modModerateSin(0),
      modQuickSin(0),
      modFastSin(0),
      startTimeLed(0),
      leds{nullptr}
{}

void StripLedManager::begin() {
    _instance = this;
    for (int i = 0; i < LED_COUNT+1; i++) {
        _instance->leds[i] = new LEDInfo{0, 0, 0, 0, 0};
    }
    _instance->strip = new Adafruit_NeoPixel(LED_COUNT, LED_PIN, NEO_GRB + NEO_KHZ800);
    _instance->strip->begin();
    _instance->strip->show(); // Initialize all pixels to 'off'
    _instance->startTimeLed = millis();
}

void StripLedManager::setLed(int index, uint8_t red, uint8_t green, uint8_t blue, uint8_t module, uint32_t delayTime) {
    if (index < 0 || index >= LED_COUNT) {
        return; // Index out of bounds
    }
    _instance->leds[index]->red = red;
    _instance->leds[index]->green = green;
    _instance->leds[index]->blue = blue;
    _instance->leds[index]->module = module;
    _instance->leds[index]->delayTime = delayTime;
    _instance->strip->setPixelColor(index, _instance->strip->Color(red, green, blue));
    _instance->strip->show();
}

void StripLedManager::calculateAnimationMode() {
    _instance->inputLoop = _instance->inputLoop + 0.01;
    if (_instance->inputLoop >= 1080)
    {
      _instance->inputLoop = 0;
    }
    _instance->modSlowSin = fabs(sin(_instance->inputLoop));
    _instance->modModerateSin = fabs(sin(_instance->inputLoop / 0.5));
    _instance->modQuickSin = fabs(sin(_instance->inputLoop / 0.25));
    _instance->modFastSin = fabs(sin(_instance->inputLoop / 0.125));
}

void StripLedManager::calculateDelayTime() {
    for (int i = 0; i < LED_COUNT; i++) {
        if (_instance->leds[i]->delayTime > 0) {
            _instance->leds[i]->delayTime -= (millis() - _instance->startTimeLed);
        }
    }
    _instance->startTimeLed = millis();
}

void StripLedManager::show() {
    _instance->calculateAnimationMode();
    _instance->calculateDelayTime();
    _instance->strip->clear();
    for (int i = 0; i < LED_COUNT; i++) {
        if (_instance->leds[i]->delayTime > 0)
        {
            switch (_instance->leds[i]->module)
            {
                case 1:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red, _instance->leds[i]->green, _instance->leds[i]->blue));
                    break;
                case 2:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red * _instance->modSlowSin, _instance->leds[i]->green * _instance->modSlowSin, _instance->leds[i]->blue * _instance->modSlowSin));
                    break;
                case 3:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red * _instance->modModerateSin, _instance->leds[i]->green * _instance->modModerateSin, _instance->leds[i]->blue * _instance->modModerateSin));
                    break;
                case 4:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red * _instance->modQuickSin, _instance->leds[i]->green * _instance->modQuickSin, _instance->leds[i]->blue * _instance->modQuickSin));
                    break;
                case 5:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red * _instance->modFastSin, _instance->leds[i]->green * _instance->modFastSin, _instance->leds[i]->blue * _instance->modFastSin));
                    break;
                default:
                    _instance->strip->setPixelColor(i, _instance->strip->Color(_instance->leds[i]->red, _instance->leds[i]->green, _instance->leds[i]->blue));
                    break;
            }
        }
    }
    _instance->strip->show();
}