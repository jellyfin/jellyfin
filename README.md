Jellyfin
============

Jellyfin is a personal media server. The Jellyfin project was started as a result of Emby's decision to take their code closed-source, as well as various philosophical differences with the core developers. Jellyfin seeks to be the free software alternative to Emby and Plex to provide media management and streaming from a dedicated server to end-user devices.

Jellyfin is descended from Emby 3.5.2, ported to the .NET Core framework, and aims to contain build facilities for every platform.

For further details, please see [our wiki](https://github.com/jellyfin/jellyfin/wiki). To receive the latest project updates feel free to join [our public chat on Matrix/Riot](https://matrix.to/#/#jellyfin:matrix.org) and to subscribe to [our subreddit](https://www.reddit.com/r/jellyfin/).

## Feature Requests

While our first priority is a stable build, we will eventually add features that were missing in Emby or were not well implemented (technically or philosophically).

[Feature Requests](http://feathub.com/jellyfin/jellyfin)

## Building Jellyfin packages

Jellyfin seeks to integrate build facilities for any desired packaging format. Instructions for the various formats can be found below.

### Debian/Ubuntu

Debian build facilities are integrated into the repo at `debian/`.

1. Install the `dotnet-sdk-2.1` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-2.1.500).
2. Run `dpkg-buildpackage -us -uc -jX`, where X is your core count.
3. Install the resulting `jellyfin*.deb` file on your system.

A huge thanks to Carlos Hernandez who created the Debian build configuration for Emby 3.1.1.

### Windows (64 bit)
A pre-built windows installer will be available at [The JellyFin Repository](https://repo.jellyfin.org/).

1. Install the dotnet core SDK 2.1 from [Microsoft's Webpage](https://dotnet.microsoft.com/download/thank-you/dotnet-sdk-2.1.500-windows-x64-installer)
2. Clone Jellyfin into a directory of your choice. From that directory run in powershell `dotnet publish -c Release -r win10-x64 MediaBrowser.sln -o $Env:APPDATA\Jellyfin-Server` or in CMD `dotnet publish -c Release -r win10-x64 MediaBrowser.sln -o %APPDATA%\Jellyfin-Server`
3. (Optional) Copy the ffmpeg binaries into the Jellyfin directory:
```
Invoke-WebRequest -Uri https://ffmpeg.zeranoe.com/builds/win64/static/ffmpeg-4.1-win64-static.zip -UseBasicParsing -OutFile $env:TEMP\fmmpeg.zip
Expand-Archive $env:TEMP\fmmpeg.zip -DestinationPath $env:TEMP\ffmpeg\
Get-ChildItem "$env:temp\ffmpeg\ffmpeg-4.1-win64-static\bin" | ForEach-Object {
    Copy-Item $_ -Destination $Env:AppData\JellyFin-Server\
}
Remove-Item $env:TEMP\ffmpeg\ -Recurse -Force
Remove-Item $env:TEMP\fmmpeg.zip -Force
```
4. (Optional) Use [NSSM](https://nssm.cc/) to configure JellyFin to run as a service
5. Jellyfin is now available in your Appdata\Roaming directory. To start it from a powershell window, `&"$env:APPDATA\Jellyfin-Server\EmbyServer.exe"`