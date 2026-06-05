FROM node:24-slim AS web-builder

# Build configuration for the jellyfin-web repository (development, production)
# For further info: https://github.com/jellyfin/jellyfin-web
ARG JELLYFIN_WEB_BUILD_CONFIG=development

# Branch/tag/commit that will be cloned to build the jellyfin-web repository
ARG JELLYFIN_WEB_GIT_REF=master

USER root

# Install build dependencies
RUN apt-get update && apt-get install git -y

# Clone the repository at the specified branch/tag/commit
RUN git clone --depth 1 --branch $JELLYFIN_WEB_GIT_REF https://github.com/jellyfin/jellyfin-web.git /root/jellyfin-web

WORKDIR /root/jellyfin-web

# Install dependencies and build
RUN npm ci --no-audit && npm run build:$JELLYFIN_WEB_BUILD_CONFIG

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

# Copy the statically built frontend artifacts
COPY --from=web-builder --chown=root:root --chmod=755 /root/jellyfin-web/dist /jellyfin/jellyfin-web

# Set environment variable needed
ENV JELLYFIN_WEB_DIR=/jellyfin/jellyfin-web \
    JELLYFIN_FFMPEG=/usr/lib/jellyfin-ffmpeg/ffmpeg

USER vscode
