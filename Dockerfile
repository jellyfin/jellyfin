ARG DOTNET_VERSION=2

FROM microsoft/dotnet:${DOTNET_VERSION}-sdk as builder
WORKDIR /repo
COPY . .
RUN export DOTNET_CLI_TELEMETRY_OPTOUT=1 \
 && dotnet clean \
 && dotnet publish \
    --configuration release \
    --output /jellyfin \
    Jellyfin.Server

FROM jrottenberg/ffmpeg:4.0-vaapi as ffmpeg
FROM microsoft/dotnet:${DOTNET_VERSION}-runtime
# libfontconfig1 is required for Skia
RUN apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests -y \
   libfontconfig1 \
 && apt-get clean autoclean \
 && apt-get autoremove \
 && rm -rf /var/lib/{apt,dpkg,cache,log}
COPY --from=ffmpeg / /
COPY --from=builder /jellyfin /jellyfin
EXPOSE 8096
VOLUME /config /media
ENTRYPOINT dotnet /jellyfin/jellyfin.dll --datadir /config
