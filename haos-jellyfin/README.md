# Jellyfin Home Assistant Add-on

Jellyfin is a Free Software Media System that puts you in control of managing and streaming your media. It is an alternative to the proprietary Emly and Plex, to provide media from a dedicated server to end-user devices via multiple apps.

## Installation

1. Add this repository URL to your Home Assistant instance.
2. Search for the "Jellyfin Media Server" add-on.
3. Click "Install".
4. Once installed, configure the add-on and click "Start".

## Hardware Acceleration

This add-on supports hardware acceleration for various GPUs.

### VA-API (Intel/AMD)

To enable VA-API:
1. Go to the Jellyfin dashboard.
2. Navigate to "Playback" -> "Transcoding".
3. Select "VA-API" as the hardware acceleration method.
4. Ensure the correct device is selected (usually `/dev/dri/renderD128`).

### Vulkan

Vulkan is also supported for compatible hardware.

## Support

For issues and feature requests, please visit the [GitHub repository](https://github.com/thcuba/haos-jellyfin).
