ARG DOTNET_VERSION=2

FROM microsoft/dotnet:${DOTNET_VERSION}-sdk as builder
WORKDIR /repo
COPY . .
RUN export DOTNET_CLI_TELEMETRY_OPTOUT=1 \
 && dotnet clean \
 && dotnet publish --configuration release --output /jellyfin

FROM microsoft/dotnet:${DOTNET_VERSION}-runtime
COPY --from=builder /jellyfin /jellyfin
EXPOSE 8096

VOLUME /config /media

ARG FFMPEG_URL=https://www.johnvansickle.com/ffmpeg/old-releases/ffmpeg-4.0.3-64bit-static.tar.xz
RUN DEBIAN_FRONTEND=noninteractive \
 apt-get update \
 && apt-get install --no-install-recommends --no-install-suggests -y \
   xz-utils \
   libfontconfig1 # Required for Skia \ 
 && curl ${FFMPEG_URL} | tar Jxf - -C /usr/bin --wildcards --strip-components=1 ffmpeg*/ffmpeg ffmpeg*/ffprobe \
 && apt-get remove --purge -y xz-utils \
 && apt-get clean autoclean \
 && apt-get autoremove \
 && rm -rf /var/lib/{apt,dpkg,cache,log}

ENTRYPOINT dotnet /jellyfin/jellyfin.dll -programdata /config
