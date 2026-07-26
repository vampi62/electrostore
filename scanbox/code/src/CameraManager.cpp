#include "CameraManager.h"
#include "config.h"
#include "Logger.h"

CameraManager::CameraManager() : initialized(false) {}

bool CameraManager::begin() {
    if (initialized) {
        return true;
    }

    camera_config_t config;
    config.ledc_channel = LEDC_CHANNEL_0;
    config.ledc_timer   = LEDC_TIMER_0;
    config.pin_d0       = CAM_PIN_D0;
    config.pin_d1       = CAM_PIN_D1;
    config.pin_d2       = CAM_PIN_D2;
    config.pin_d3       = CAM_PIN_D3;
    config.pin_d4       = CAM_PIN_D4;
    config.pin_d5       = CAM_PIN_D5;
    config.pin_d6       = CAM_PIN_D6;
    config.pin_d7       = CAM_PIN_D7;
    config.pin_xclk     = CAM_PIN_XCLK;
    config.pin_pclk     = CAM_PIN_PCLK;
    config.pin_vsync    = CAM_PIN_VSYNC;
    config.pin_href     = CAM_PIN_HREF;
    config.pin_sscb_sda = CAM_PIN_SIOD;
    config.pin_sscb_scl = CAM_PIN_SIOC;
    config.pin_pwdn     = CAM_PIN_PWDN;
    config.pin_reset    = CAM_PIN_RESET;
    config.xclk_freq_hz = CAM_XCLK_FREQ_HZ;
    config.pixel_format = PIXFORMAT_JPEG;

    if (psramFound()) {
        config.frame_size   = CAM_FRAME_SIZE_PSRAM;
        config.jpeg_quality = CAM_JPEG_QUALITY_PSRAM;
        config.fb_count     = 2;
        config.fb_location  = CAMERA_FB_IN_PSRAM;
        config.grab_mode    = CAMERA_GRAB_LATEST;
    } else {
        config.frame_size   = CAM_FRAME_SIZE_NOPSRAM;
        config.jpeg_quality = CAM_JPEG_QUALITY_NOPSRAM;
        config.fb_count     = 1;
        config.fb_location  = CAMERA_FB_IN_DRAM;
        config.grab_mode    = CAMERA_GRAB_WHEN_EMPTY;
        Logger::warning("Camera: no PSRAM found, falling back to reduced resolution/buffers");
    }

    esp_err_t err = esp_camera_init(&config);
    if (err != ESP_OK) {
        Logger::error("Camera init failed with error 0x" + String(err, HEX));
        initialized = false;
        return false;
    }

    initialized = true;
    Logger::info("Camera ready - sensor: " + getSensorName() + ", resolution: " + getFrameSizeName());
    return true;
}

camera_fb_t* CameraManager::capture() {
    if (!initialized) {
        return nullptr;
    }
    camera_fb_t* fb = esp_camera_fb_get();
    if (!fb) {
        Logger::error("Camera: frame capture failed");
    }
    return fb;
}

void CameraManager::release(camera_fb_t* fb) {
    if (fb) {
        esp_camera_fb_return(fb);
    }
}

bool CameraManager::setFrameSize(framesize_t size) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_framesize(s, size) == 0;
}

bool CameraManager::setQuality(int quality) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_quality(s, constrain(quality, 4, 63)) == 0;
}

bool CameraManager::setBrightness(int level) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_brightness(s, constrain(level, -2, 2)) == 0;
}

bool CameraManager::setContrast(int level) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_contrast(s, constrain(level, -2, 2)) == 0;
}

bool CameraManager::setSaturation(int level) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_saturation(s, constrain(level, -2, 2)) == 0;
}

bool CameraManager::setHMirror(bool enabled) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_hmirror(s, enabled ? 1 : 0) == 0;
}

bool CameraManager::setVFlip(bool enabled) {
    if (!initialized) {
        return false;
    }
    sensor_t* s = esp_camera_sensor_get();
    return s && s->set_vflip(s, enabled ? 1 : 0) == 0;
}

String CameraManager::getFrameSizeName() const {
    sensor_t* s = esp_camera_sensor_get();
    if (!s) {
        return "UNKNOWN";
    }
    switch (s->status.framesize) {
        case FRAMESIZE_96X96:   return "96x96";
        case FRAMESIZE_QQVGA:   return "QQVGA (160x120)";
        case FRAMESIZE_QCIF:    return "QCIF (176x144)";
        case FRAMESIZE_HQVGA:   return "HQVGA (240x176)";
        case FRAMESIZE_240X240: return "240x240";
        case FRAMESIZE_QVGA:    return "QVGA (320x240)";
        case FRAMESIZE_CIF:     return "CIF (400x296)";
        case FRAMESIZE_HVGA:    return "HVGA (480x320)";
        case FRAMESIZE_VGA:     return "VGA (640x480)";
        case FRAMESIZE_SVGA:    return "SVGA (800x600)";
        case FRAMESIZE_XGA:     return "XGA (1024x768)";
        case FRAMESIZE_HD:      return "HD (1280x720)";
        case FRAMESIZE_SXGA:    return "SXGA (1280x1024)";
        case FRAMESIZE_UXGA:    return "UXGA (1600x1200)";
        case FRAMESIZE_QXGA:    return "QXGA (2048x1536)";
        default:                return "UNKNOWN";
    }
}

int CameraManager::getSensorPID() const {
    sensor_t* s = esp_camera_sensor_get();
    return s ? s->id.PID : 0;
}

String CameraManager::getSensorName() const {
    switch (getSensorPID()) {
        case OV2640_PID: return "OV2640";
        case OV3660_PID: return "OV3660";
        case OV5640_PID: return "OV5640";
        default:         return "Unknown (0x" + String(getSensorPID(), HEX) + ")";
    }
}
