# The Jellyfin Docker image with the server of this fork. It starts from an official Jellyfin image, for its web
# client, FFmpeg and setup, and replaces the server with one built from this checkout. The Compose examples next to
# this file build it; the build context is the root of the repository:
#
#   docker build -f docs/database/compose/Dockerfile -t jellyfin-multidb:local .
#
# The base is an unstable (nightly) image of upstream Jellyfin from the date this fork was last synced with
# upstream, so that its web client matches the server. Pinned by digest.
ARG JELLYFIN_BASE=jellyfin/jellyfin:2026101212@sha256:14b084dc840e0fd64597cb8a57c338ee4594502b9e367b6376e06ae11e85179b

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:10.0.401@sha256:e70cdb7f80b0348f5cb85f19a8f670fca061f033d57eed12fa003d58b0e06317 AS build
ARG TARGETARCH
WORKDIR /src
COPY . .
RUN case "$TARGETARCH" in \
        amd64) runtime=linux-x64 ;; \
        arm64) runtime=linux-arm64 ;; \
        *) echo "There is no Jellyfin build for $TARGETARCH" >&2; exit 1 ;; \
    esac \
    && dotnet publish Jellyfin.Server --configuration Release --runtime "$runtime" --self-contained \
        --output /out -p:DebugType=none -p:DebugSymbols=false

FROM ${JELLYFIN_BASE}
# Everything of the official server goes, except the web client.
RUN find /jellyfin -mindepth 1 -maxdepth 1 ! -name jellyfin-web -exec rm -rf {} +
COPY --from=build /out/ /jellyfin/

# Compose mounts database.xml at /run/jellyfin/database.xml. It is copied into the configuration directory on the
# first start only, so that the copy stays an ordinary file: Jellyfin may rewrite it, and so does the restore of a
# backup archive, which a read-only mount would break. To change the configuration later, edit the copy in the
# configuration volume, or delete it to have the mounted file copied again.
COPY --chmod=755 <<'EOF' /usr/local/bin/jellyfin-with-database-config
#!/bin/sh
set -eu
seed=/run/jellyfin/database.xml
target="${JELLYFIN_CONFIG_DIR:-/config/config}/database.xml"
if [ -f "$seed" ] && [ ! -e "$target" ]; then
    mkdir -p "$(dirname "$target")"
    cp "$seed" "$target"
    echo "Copied $seed to $target"
fi
exec "$@"
EOF

ENTRYPOINT ["/usr/local/bin/jellyfin-with-database-config", "/jellyfin/jellyfin"]
