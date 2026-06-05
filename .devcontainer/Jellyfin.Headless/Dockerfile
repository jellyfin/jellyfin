FROM mcr.microsoft.com/devcontainers/dotnet:10.0-noble AS debug

USER root

# Import Jellyfin public GPG key
RUN curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key | gpg --batch --yes --dearmor -o /etc/apt/keyrings/jellyfin.gpg

# Create Jellyfin source file
RUN . /etc/os-release && ARCH=$(dpkg --print-architecture) && \
    tee /etc/apt/sources.list.d/jellyfin.sources <<EOF
Types: deb
URIs: https://repo.jellyfin.org/$ID
Suites: $VERSION_CODENAME
Components: main
Architectures: $ARCH
Signed-By: /etc/apt/keyrings/jellyfin.gpg
EOF

# Install ffmpeg
RUN apt-get update && \
    apt-get install -y \
        jellyfin-ffmpeg7 \
        && rm -rf /var/lib/apt/lists/*

# Set environment variable needed
ENV JELLYFIN_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg

USER vscode
