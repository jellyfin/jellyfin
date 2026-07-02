# Build the server from our fork
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish Jellyfin.Server \
    --arch x64 \
    --configuration Release \
    --output /server \
    --self-contained \
    -p:DebugSymbols=false \
    -p:DebugType=none

# Use the official Jellyfin image as base (has ffmpeg, web UI, all deps)
# and just replace the server binaries
FROM jellyfin/jellyfin:unstable
COPY --from=build /server /jellyfin
