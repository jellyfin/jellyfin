FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS debug

ARG VSCODE_UID=1000

USER root

# Add the dotnet group, set update permissions for /usr/share/dotnet
# and finally add the vscode user with provided UID
RUN groupadd dotnet && \
    chown -R root:dotnet /usr/share/dotnet && \
    chmod -R g+w /usr/share/dotnet && \
    useradd -m -G dotnet -u $VSCODE_UID -s /bin/bash vscode

# Install needed packages
RUN apt-get update && apt-get install -y \
    curl \
    git \
    gnupg2 \
    libfontconfig1

# Import Jellyfin public GPG key
RUN curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key | gpg --dearmor -o /etc/apt/keyrings/jellyfin.gpg

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
