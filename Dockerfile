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

ARG JELLYFIN_WEB_VERSION=10.2.2
RUN curl -L https://github.com/jellyfin/jellyfin-web/archive/v${JELLYFIN_WEB_VERSION}.tar.gz | tar zxf - \
 && rm -rf /jellyfin/jellyfin-web \
 && mv jellyfin-web-${JELLYFIN_WEB_VERSION} /jellyfin/jellyfin-web

EXPOSE 8096
VOLUME /cache /config /media
ENTRYPOINT dotnet /jellyfin/jellyfin.dll \
    --datadir /config \
    --cachedir /cache \
    --ffmpeg /usr/local/bin/ffmpeg
