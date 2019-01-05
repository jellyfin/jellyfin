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

RUN apt update
RUN apt install -y wget xz-utils
RUN mkdir /ffmpeg
WORKDIR /ffmpeg
RUN wget https://www.johnvansickle.com/ffmpeg/old-releases/ffmpeg-4.0.3-64bit-static.tar.xz
RUN tar xf /ffmpeg/ffmpeg-4.0.3-64bit-static.tar.xz
RUN ln -s /ffmpeg/ffmpeg-4.0.3-64bit-static/ffmpeg /usr/local/sbin/ffmpeg
RUN ln -s /ffmpeg/ffmpeg-4.0.3-64bit-static/ffprobe /usr/local/sbin/ffprobe


ENTRYPOINT if [ -n "$PUID$PGUID" ]; \
    then echo "PUID/PGID are deprecated. Use Docker user param." >&2; exit 1; \
    else dotnet /jellyfin/jellyfin.dll -programdata /config; fi
