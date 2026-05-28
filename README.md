# Jellyfin for HAOS - With Hardware Acceleration

Jellyfin is a free software media system that puts you in control of managing and streaming your media.

## Key Features
- Full hardware transcoding support (VA-API & Vulkan)
- H.264, H.265, VP9, AV1 decoding
- Multi-user support with parental controls
- DLNA, Chromecast, and mobile apps
- Direct integration with Home Assistant OS

## Hardware Acceleration Setup

### For Intel/AMD GPUs (VA-API)
The add-on automatically uses VA-API when available. Ensure:
1. Your GPU is supported by the Mesa VA-API drivers
2. The `/dev/dri` device is accessible (handled by HAOS)

### For Modern GPUs (Vulkan)
Vulkan support is enabled by default for:
- AMD GPUs (RADV driver)
- Intel GPUs (Intel ANV driver)
- NVIDIA GPUs (via Nouveau - limited support)

### To Enable in Jellyfin UI
1. Go to Dashboard → Playback → Transcoding
2. Set Hardware Acceleration to:
   - **VA-API** (for Intel/AMD)
   - **Vulkan** (for AMD/Intel)
3. Click Save

### Verifying Hardware Acceleration
Check the Jellyfin dashboard or logs for:
```
Hardware acceleration: VA-API enabled
Hardware acceleration: Vulkan enabled
```

## Installation
1. Add this repository to Home Assistant OS Supervisor
2. Install the "Jellyfin Media Server" add-on
3. Start the add-on and configure your media folders
4. Enable hardware acceleration in Jellyfin Dashboard

## Support
For issues, please visit: https://github.com/jellyfin/jellyfin