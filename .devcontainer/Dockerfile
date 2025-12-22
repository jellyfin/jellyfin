FROM node:20-slim AS web-builder

ARG JELLYFIN_WEB_BUILD_CONFIG=development
ARG JELLYFIN_WEB_GIT_BRANCH=master

USER root

# Intall build dependencies
RUN apt-get update && apt-get install git -y

# Clone the repository at the specified branch/tag/commit
RUN git clone --branch $JELLYFIN_WEB_GIT_BRANCH https://github.com/jellyfin/jellyfin-web.git /root/jellyfin-web

WORKDIR /root/jellyfin-web

# Install dependencies and build
RUN npm i && npm run build:$JELLYFIN_WEB_BUILD_CONFIG

FROM mcr.microsoft.com/devcontainers/dotnet:9.0-bookworm AS debug

USER root

# Import Jellyfin public GPG key
RUN curl -fsSL https://repo.jellyfin.org/jellyfin_team.gpg.key | sudo gpg --dearmor -o /etc/apt/keyrings/jellyfin.gpg

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

# Install needed packages
RUN apt-get update && \
    apt-get install \
        jellyfin-ffmpeg7 \
        libfontconfig1 \
        -y

# Create symbolic links for ffmpeg tools
RUN ln -s /usr/lib/jellyfin-ffmpeg/ffmpeg /usr/local/bin/ffmpeg && \
    ln -s /usr/lib/jellyfin-ffmpeg/ffprobe /usr/local/bin/ffprobe && \
    ln -s /usr/lib/jellyfin-ffmpeg/vainfo /usr/local/bin/vainfo

# Copy the statically compiled frontend artifacts
COPY --from=web-builder --chown=root:root --chmod=755 /root/jellyfin-web/dist /srv/jellyfin-web

# Set environment variable needed for serving the frontend
ENV JELLYFIN_WEB_DIR=/srv/jellyfin-web
