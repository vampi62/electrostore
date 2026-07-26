#ifndef LOGGER_H
#define LOGGER_H

#include <Arduino.h>

class Logger {
public:
    static void info(const String& message) {
        Serial.println("[INFO] " + message);
    }
    
    static void warning(const String& message) {
        Serial.println("[WARN] " + message);
    }
    
    static void error(const String& message) {
        Serial.println("[ERROR] " + message);
    }

    static void debug(const String& message) {
        Serial.println("[DEBUG] " + message);
    }
};

#endif