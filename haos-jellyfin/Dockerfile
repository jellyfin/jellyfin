# Jellyfin for HAOS with FFmpeg and Vulkan support
# Base image: linuxserver/jellyfin (highly optimized)

FROM linuxserver/jellyfin:latest

# Install additional tools and hardware acceleration drivers
# mesa-vulkan-drivers: Support for Vulkan
# libva-drm2 & vainfo: Support for VA-API (Intel/AMD hardware transcoding)
# ffmpeg: Already included in linuxserver image, but we ensure dependencies are met
RUN apt-get update && apt-get install -y --no-install-recommends \
    jq \
    curl \
    gnupg2 \
    mesa-vulkan-drivers \
    libva-drm2 \
    vainfo \
    && rm -rf /var/lib/apt/lists/*

# Ensure the jellyfin user has permissions for GPU access
# (In HAOS/Docker, this is often handled by passing the device)

# Expose Jellyfin port
EXPOSE 8096


# Run jellyfin with proper settings
CMD ["/init"]