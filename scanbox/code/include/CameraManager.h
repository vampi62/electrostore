#ifndef CAMERA_MANAGER_H
#define CAMERA_MANAGER_H

#include <Arduino.h>
#include "esp_camera.h"

class CameraManager {
private:
    bool initialized;

public:
    explicit CameraManager();

    bool begin();
    bool isInitialized() const { return initialized; }

    camera_fb_t* capture();
    void release(camera_fb_t* fb);

    bool setFrameSize(framesize_t size);
    bool setQuality(int quality);
    bool setBrightness(int level);
    bool setContrast(int level);
    bool setSaturation(int level);
    bool setHMirror(bool enabled);
    bool setVFlip(bool enabled);

    String getFrameSizeName() const;
    int getSensorPID() const;
    String getSensorName() const;
};

#endif
