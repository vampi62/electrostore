# Documentation for the LedStorage Module

## Required Materials List

| Quantity | Component |
|----------|-----------|
| 1 | ESP-01 (ESP8266) |
| X | WS2812B |
| 1 | WS2812 LED controller for ESP01 |
| 1 | Micro-USB connector |
| 1 | 3D case to print |
| 1 | 3D support to fix the LEDs on the storage space |

## Wiring Diagram
![Wiring Diagram](/docs/assets/espLedStoreWire.PNG)

## 3D File to Print and Assembly

## First Upload with Arduino IDE
1. Install the Arduino IDE from the [official website](https://www.arduino.cc/en/software).
2. Add support for the ESP8266 by following [this guide](https://arduino-esp8266.readthedocs.io/en/latest/installing.html).
3. Open the Arduino IDE and select the correct COM port and ESP8266 model.
4. Download the initial code from your project's GitHub repository.
   - Make sure you have the necessary libraries installed, including `ESPAsyncWebServer`, `ESPAsyncTCP`, and `Adafruit_NeoPixel`.
5. Compile and upload the code to the ESP8266.
6. After a while, the ESP8266 will create its own WiFi network (SSID: "ESP_Config", password: "ConfigPass").
7. Connect to this network, open a browser, and enter the address `http://192.168.4.1` to access the configuration page.

### User Settings
Configure a password for the user to secure access to the various configuration pages. If the ESP is connected to a WiFi network, user settings will be required to access the configuration pages.
If the ESP is not connected to a WiFi network, user settings will not be required to access the configuration pages, so you can change the credentials if you lose the password.

### OTA Settings
Enable OTA to update the module's code from the Arduino IDE without having to physically reprogram it.

## OTA Update in the IDE

1. Ensure that the ESP is connected to the same WiFi network as your computer.
2. In the Arduino IDE, go to `Tools > Port` and select the ESP's network port.
3. Download the new code with the OTA update.
4. If no user password is configured, the code to enter is "0".

## Information LED
the first LED on the WS2812B strip will be used to indicate the status of the ESP module. The colors and their meanings are as follows:

| Color | Status |
|---------|------|
| White | Initialization |
| Cyan | Successful WiFi connection |
| Red | No WiFi connection |
| Green | Successful MQTT connection |
| Purple | No MQTT connection |

# Documentation for the Scanner Module

## Required Materials List

| Quantity | Component |
|----------|-----------|
| 1 | ESP32cam |
| 1 | WS2812B |
| 1 | RING LED WS2812B |
| 1 | ON/OFF switch |
| 1 | Micro-USB connector |
| 1 | 3D case to print |

## Wiring Diagram
![Wiring Diagram](/docs/assets/espScanStoreWire.PNG)

## 3D File to Print and Assembly

## First Upload with Arduino IDE

1. Install the Arduino IDE from the [official website](https://www.arduino.cc/en/software).
2. Add support for the ESP32 by following [this guide](https://docs.espressif.com/projects/arduino-esp32/en/latest/installing.html).
3. Open the Arduino IDE and select the correct COM port and ESP32 model.
4. Download the initial code from your project's GitHub repository.
   - Make sure you have the necessary libraries installed, including `ESPAsyncWebServer`, `ESPAsyncTCP`, and `Adafruit_NeoPixel`.
5. Compile and upload the code to the ESP32.
6. After a while, the ESP32 will create its own WiFi network (SSID: "ESP_Config", password: "ConfigPass").
7. Connect to this network, open a browser, and enter the address `http://192.168.4.1` to access the configuration page.

### User Settings
Configure a password for the user to secure access to the various configuration pages. If the ESP is connected to a WiFi network, user settings will be required to access the configuration pages.
If the ESP is not connected to a WiFi network, user settings will not be required to access the configuration pages, so you can change the credentials if you lose the password.

### OTA Settings
Enable OTA to update the module's code from the Arduino IDE without having to physically reprogram it.

## OTA Update in the IDE

1. Ensure that the ESP is connected to the same WiFi network as your computer.
2. In the Arduino IDE, go to `Tools > Port` and select the ESP's network port.
3. Download the new code with the OTA update.
4. If no user password is configured, the code to enter is "0".

## Information LED
the first LED on the WS2812B strip will be used to indicate the status of the ESP module. The colors and their meanings are as follows:

| Color | Status |
|---------|------|
| White | Initialization |
| Cyan | Successful WiFi connection |
| Red | No WiFi connection |