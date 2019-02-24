ARG DOTNET_VERSION=2

FROM microsoft/dotnet:${DOTNET_VERSION}-sdk as builder
WORKDIR /repo
COPY . .
ENV DOTNET_CLI_TELEMETRY_OPTOUT=1
RUN bash -c "source deployment/common.build.sh && \
    build_jellyfin Jellyfin.Server Release linux-x64 /jellyfin"

FROM jellyfin/ffmpeg as ffmpeg
FROM microsoft/dotnet:${DOTNET_VERSION}-runtime
# libfontconfig1 is required for Skia
RUN apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests -y \
   libfontconfig1 \
 && apt-get clean autoclean \
 && apt-get autoremove \
 && rm -rf /var/lib/{apt,dpkg,cache,log} \
 && mkdir -p /cache /config /media \
 && chmod 777 /cache /config /media
COPY --from=ffmpeg / /
COPY --from=builder /jellyfin /jellyfin
EXPOSE 8096
VOLUME /cache /config /media
ENTRYPOINT dotnet /jellyfin/jellyfin.dll --datadir /config --cachedir /cache
