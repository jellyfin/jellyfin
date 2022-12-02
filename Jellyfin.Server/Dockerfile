#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /jellyfin
EXPOSE 8096

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS server
WORKDIR /src
COPY ["nuget.config", "."]
COPY ["Jellyfin.Server/Jellyfin.Server.csproj", "Jellyfin.Server/"]
COPY ["Emby.Drawing/Emby.Drawing.csproj", "Emby.Drawing/"]
COPY ["MediaBrowser.Model/MediaBrowser.Model.csproj", "MediaBrowser.Model/"]
COPY ["Jellyfin.Data/Jellyfin.Data.csproj", "Jellyfin.Data/"]
COPY ["src/Jellyfin.Extensions/Jellyfin.Extensions.csproj", "src/Jellyfin.Extensions/"]
COPY ["MediaBrowser.Controller/MediaBrowser.Controller.csproj", "MediaBrowser.Controller/"]
COPY ["Emby.Naming/Emby.Naming.csproj", "Emby.Naming/"]
COPY ["MediaBrowser.Common/MediaBrowser.Common.csproj", "MediaBrowser.Common/"]
COPY ["Emby.Server.Implementations/Emby.Server.Implementations.csproj", "Emby.Server.Implementations/"]
COPY ["Emby.Notifications/Emby.Notifications.csproj", "Emby.Notifications/"]
COPY ["Jellyfin.Api/Jellyfin.Api.csproj", "Jellyfin.Api/"]
COPY ["Emby.Dlna/Emby.Dlna.csproj", "Emby.Dlna/"]
COPY ["RSSDP/RSSDP.csproj", "RSSDP/"]
COPY ["Jellyfin.Networking/Jellyfin.Networking.csproj", "Jellyfin.Networking/"]
COPY ["src/Jellyfin.MediaEncoding.Hls/Jellyfin.MediaEncoding.Hls.csproj", "src/Jellyfin.MediaEncoding.Hls/"]
COPY ["src/Jellyfin.MediaEncoding.Keyframes/Jellyfin.MediaEncoding.Keyframes.csproj", "src/Jellyfin.MediaEncoding.Keyframes/"]
COPY ["Jellyfin.Server.Implementations/Jellyfin.Server.Implementations.csproj", "Jellyfin.Server.Implementations/"]
COPY ["MediaBrowser.Providers/MediaBrowser.Providers.csproj", "MediaBrowser.Providers/"]
COPY ["DvdLib/DvdLib.csproj", "DvdLib/"]
COPY ["MediaBrowser.XbmcMetadata/MediaBrowser.XbmcMetadata.csproj", "MediaBrowser.XbmcMetadata/"]
COPY ["MediaBrowser.LocalMetadata/MediaBrowser.LocalMetadata.csproj", "MediaBrowser.LocalMetadata/"]
COPY ["Emby.Photos/Emby.Photos.csproj", "Emby.Photos/"]
COPY ["MediaBrowser.MediaEncoding/MediaBrowser.MediaEncoding.csproj", "MediaBrowser.MediaEncoding/"]
COPY ["Jellyfin.Drawing.Skia/Jellyfin.Drawing.Skia.csproj", "Jellyfin.Drawing.Skia/"]
RUN dotnet restore "Jellyfin.Server/Jellyfin.Server.csproj"
COPY . . 
WORKDIR "/src/Jellyfin.Server"
RUN dotnet publish --disable-parallel "Jellyfin.Server.csproj" -c Release -o /publish --self-contained --runtime linux-x64

FROM bitnami/minideb:latest as web
RUN apt-get update && apt-get -y install git gnupg wget apt-transport-https curl autoconf g++ make libpng-dev gifsicle automake libtool make gcc musl-dev nasm npm
RUN cd /opt && git clone https://github.com/jellyfin/jellyfin-web.git
RUN cd /opt/jellyfin-web && npm install

FROM base AS final
WORKDIR /jellyfin
COPY --from=server /publish .
COPY --from=web /opt/jellyfin-web/dist ./jellyfin-web
ENTRYPOINT ["dotnet", "jellyfin.dll"]
