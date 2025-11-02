
# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY . .

# Restore dependencies
RUN dotnet restore Jellyfin.Server/Jellyfin.Server.csproj

# Build the application
RUN dotnet publish Jellyfin.Server/Jellyfin.Server.csproj \
    -c Release \
    -o /app/jellyfin \
    --no-restore \
    -p:DebugType=none

# Web UI stage
FROM node:alpine AS web
ARG JELLYFIN_WEB_VERSION=10.12.0
WORKDIR /web

# Download and extract jellyfin-web from GitHub releases
RUN apk add --no-cache wget tar && \
    wget -O jellyfin-web.tar.gz "https://github.com/jellyfin/jellyfin-web/releases/download/v${JELLYFIN_WEB_VERSION}/jellyfin-web_${JELLYFIN_WEB_VERSION}.tar.gz" && \
    tar -xzf jellyfin-web.tar.gz -C /web && \
    rm jellyfin-web.tar.gz

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /jellyfin

# Install FFmpeg, PostgreSQL client tools, and other dependencies
RUN apt-get update && \
    apt-get install -y --no-install-recommends \
    ffmpeg \
    libfontconfig1 \
    libfreetype6 \
    libssl3 \
    ca-certificates \
    postgresql-client && \
    rm -rf /var/lib/apt/lists/*

# Copy application
COPY --from=build /app/jellyfin .

# Copy Web UI
COPY --from=web /web /jellyfin/jellyfin-web

# Create directories
RUN mkdir -p /config /cache /media && \
    chmod 777 /config /cache /media

# Environment variables for PostgreSQL
ENV JELLYFIN_DATA_DIR=/config/data \
    JELLYFIN_CACHE_DIR=/cache \
    JELLYFIN_LOG_DIR=/config/log \
    JELLYFIN_CONFIG_DIR=/config \
    JELLYFIN_POSTGRES_HOST=postgres \
    JELLYFIN_POSTGRES_PORT=5432 \
    JELLYFIN_POSTGRES_DATABASE=jellyfin \
    JELLYFIN_POSTGRES_USER=jellyfin \
    JELLYFIN_POSTGRES_PASSWORD=jellyfin

# Expose ports
EXPOSE 8096 8920

# Create entrypoint script
RUN echo '#!/bin/bash\n\
set -e\n\
\n\
# Generate PostgreSQL connection string from environment variables\n\
export JELLYFIN_POSTGRES_CONNECTION_STRING="Host=${JELLYFIN_POSTGRES_HOST};Port=${JELLYFIN_POSTGRES_PORT};Database=${JELLYFIN_POSTGRES_DATABASE};Username=${JELLYFIN_POSTGRES_USER};Password=${JELLYFIN_POSTGRES_PASSWORD}"\n\
\n\
# Create database configuration if it does not exist\n\
if [ ! -f "${JELLYFIN_CONFIG_DIR}/database.xml" ]; then\n\
  echo "Creating PostgreSQL database configuration..."\n\
  cat > "${JELLYFIN_CONFIG_DIR}/database.xml" <<EOF\n\
<?xml version="1.0" encoding="utf-8"?>\n\
<DatabaseConfigurationOptions xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance" xmlns:xsd="http://www.w3.org/2001/XMLSchema">\n\
  <DatabaseType>Jellyfin-PostgreSQL</DatabaseType>\n\
  <LockingBehavior>NoLock</LockingBehavior>\n\
</DatabaseConfigurationOptions>\n\
EOF\n\
fi\n\
\n\
echo "Starting Jellyfin with PostgreSQL backend..."\n\
echo "PostgreSQL Host: ${JELLYFIN_POSTGRES_HOST}:${JELLYFIN_POSTGRES_PORT}"\n\
echo "Database: ${JELLYFIN_POSTGRES_DATABASE}"\n\
\n\
exec dotnet jellyfin.dll \\\n\
  --datadir "${JELLYFIN_DATA_DIR}" \\\n\
  --configdir "${JELLYFIN_CONFIG_DIR}" \\\n\
  --cachedir "${JELLYFIN_CACHE_DIR}" \\\n\
  --logdir "${JELLYFIN_LOG_DIR}" \\\n\
  --webdir /jellyfin/jellyfin-web\n\
' > /entrypoint.sh && chmod +x /entrypoint.sh

VOLUME ["/config", "/cache", "/media"]

ENTRYPOINT ["/entrypoint.sh"]
