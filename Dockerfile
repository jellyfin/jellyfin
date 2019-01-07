ARG DOTNET_VERSION=2


# Download ffmpeg first to allow quicker rebuild of other layers
FROM alpine as ffmpeg
ARG FFMPEG_URL=https://www.johnvansickle.com/ffmpeg/old-releases/ffmpeg-4.0.3-64bit-static.tar.xz
RUN wget ${FFMPEG_URL} -O - | tar Jxf - \
 && mkdir ffmpeg-bin \
 && mv ffmpeg*/ffmpeg ffmpeg-bin \
 && mv ffmpeg*/ffprobe ffmpeg-bin


FROM microsoft/dotnet:${DOTNET_VERSION}-sdk as builder
WORKDIR /repo
COPY . .
RUN export DOTNET_CLI_TELEMETRY_OPTOUT=1 \
 && dotnet clean \
 && dotnet publish --configuration release --output /jellyfin


FROM microsoft/dotnet:${DOTNET_VERSION}-runtime
COPY --from=builder /jellyfin /jellyfin
COPY --from=ffmpeg /ffmpeg-bin/* /usr/bin/
EXPOSE 8096
VOLUME /config /media
RUN apt update \
 && apt install -y libfontconfig1  # needed for Skia
ENTRYPOINT dotnet /jellyfin/jellyfin.dll -programdata /config
