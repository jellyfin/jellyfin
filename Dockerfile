# Jellyfin Add-on for Home Assistant OS
# Based on linuxserver/jellyfin image with additional tools

FROM linuxserver/jellyfin:latest

# Install additional tools for Home Assistant integration
RUN apt-get update && apt-get install -y --no-install-recommends \
    jq \
    curl \
    gnupg2 \
    && rm -rf /var/lib/apt/lists/*

# Copy custom configuration if needed
COPY config.yaml /config/jellyfin/config.yaml

# Expose Jellyfin port
EXPOSE 8096

# Volume for media and config
VOLUME ["/media", "/config/jellyfin"]

# Run jellyfin with proper settings
CMD ["/init"]