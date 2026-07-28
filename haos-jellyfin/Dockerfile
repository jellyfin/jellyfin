# Jellyfin for HAOS with Hardware Acceleration Support
# Base image: linuxserver/jellyfin (highly optimized)
# Pinned to specific version for reproducibility
FROM linuxserver/jellyfin:10.11.7

# Install additional tools and hardware acceleration drivers
# mesa-vulkan-drivers: Support for Vulkan (AMD/Intel)
# libva-drm2: Support for VA-API (Intel/AMD hardware transcoding)
# libvulkan1: Vulkan runtime support
# Pinned versions for reproducibility
RUN apt-get update && apt-get install -y --no-install-recommends \
    jq=1.7.1-3 \
    curl=8.5.0-2ubuntu10.5 \
    mesa-vulkan-drivers=24.0.6-1ubuntu1.24.04.1 \
    libva-drm2=2.22.0-1 \
    libvulkan1=1.3.283.0-1 \
    && rm -rf /var/lib/apt/lists/* /tmp/* /var/tmp/*

# Intel hardware acceleration: use intel-media-driver (iHD) when present
# Fallback to i965/legacy if iHD is unavailable
ENV LIBVA_DRIVER_NAME=iHD

# Create render group if not exists and ensure jellyfin user has permissions for GPU access
RUN getent group render >/dev/null 2>&1 || groupadd -r render && \
    usermod -a -G video,render jellyfin

# Expose Jellyfin port
EXPOSE 8096

# Health check using Jellyfin's public API endpoint
HEALTHCHECK --interval=30s --timeout=10s --start-period=5s --retries=3 \
    CMD curl -f --silent http://localhost:8096/System/Info/Public || exit 1

# Run jellyfin with proper settings
CMD ["/init"]