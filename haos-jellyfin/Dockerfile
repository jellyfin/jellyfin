# Jellyfin for HAOS with Hardware Acceleration Support
# Base image: linuxserver/jellyfin (highly optimized)
# Pinned to specific version for reproducibility
FROM linuxserver/jellyfin:10.11.7

# Install additional tools and hardware acceleration drivers
# mesa-vulkan-drivers: Support for Vulkan (AMD/Intel)
# libva-drm2: Support for VA-API (Intel/AMD hardware transcoding)
# libvulkan1: Vulkan runtime support
# Using latest available versions from base image repositories for compatibility
RUN apt-get update && apt-get install -y --no-install-recommends \
    jq \
    curl \
    mesa-vulkan-drivers \
    libva-drm2 \
    libvulkan1 \
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