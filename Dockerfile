ARG DOTNET_VERSION=2

FROM microsoft/dotnet:${DOTNET_VERSION}-sdk as builder
WORKDIR /repo
COPY . .
RUN export DOTNET_CLI_TELEMETRY_OPTOUT=1 \
 && dotnet clean \
 && dotnet publish --configuration release --output /jellyfin

FROM microsoft/dotnet:${DOTNET_VERSION}-runtime
COPY --from=builder /jellyfin /jellyfin
RUN apt update \
 && apt install -y ffmpeg gosu
EXPOSE 8096
VOLUME /config /media
ENV PUID=1000 PGID=1000
ENTRYPOINT chown $PUID:$PGID /config /media \
 && gosu $PUID:$PGID dotnet /jellyfin/jellyfin.dll -programdata /config
