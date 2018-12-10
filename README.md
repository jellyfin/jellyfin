Jellyfin
============

Jellyfin is a personal media server. The Jellyfin project was started as a result of Emby's decision to take their code closed-source, as well as various philosophical differences with the core developers. Jellyfin seeks to be the free software alternative to Emby and Plex to provide media management and streaming from a dedicated server to end-user devices.

Jellyfin is descended from Emby 3.5.2, ported to the .NET Core framework, and aims to contain build facilities for every platform.

For further details, please see [our wiki](https://github.com/jellyfin/jellyfin/wiki) and join [our public chat on Matrix/Riot](https://matrix.to/#/#jellyfin:matrix.org).

## Building Jellyfin packages

Jellyfin seeks to integrate build facilities for any desired packaging format. Instructions for the various formats can be found below.

### Debian/Ubuntu

Debian build facilities are integrated into the repo at `debian/`.

1. Install the `dotnet-sdk-2.1` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/linux-package-manager/debian9/sdk-2.1.500).
2. Run `dpkg-buildpackage -us -uc -jX`, where X is your core count.
3. Install the resulting `jellyfin*.deb` file on your system.

A huge thanks to Carlos Hernandez who created the Debian build configuration for Emby 3.1.1, which is forward-ported to 3.4.1 in this repository. His repository is [here](https://download.opensuse.org/repositories/home:/emby/Debian_9.0/) and contains stock packages for the 3.1.1 release.
