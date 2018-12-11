Jellyfin
============

Jellyfin is a personal media server. The Jellyfin project was started as a result of Emby's decision to take their code closed-source, as well as various philosophical differences with the core developers. Jellyfin seeks to be the free software alternative to Emby and Plex to provide media management and streaming from a dedicated server to end-user devices.

Jellyfin is descended from Emby 3.5.2, ported to the .NET Core framework, and aims to contain build facilities for every platform.

For further details, please see [our wiki](https://github.com/jellyfin/jellyfin/wiki). To receive the latest project updates feel free to join [our public chat on Matrix/Riot](https://matrix.to/#/#jellyfin:matrix.org) and to subscribe to [our subreddit](https://www.reddit.com/r/jellyfin/).

## Feature Requests

While our first priority is a stable build, we will eventually add features that were missing in Emby or were not well implemented (technically or philosophically).

[Feature Requests](http://feathub.com/jellyfin/jellyfin)

## Prebuilt Jellyfin packages

Prebuild packages are available for Debian/Ubuntu and Arch, and via Docker Hub.

### Docker

The Jellyfin Docker image is available on Docker Hub at https://hub.docker.com/r/jellyfin/jellyfin/

### Arch

The Jellyfin package is in the AUR at https://aur.archlinux.org/packages/jellyfin-git/

### Debian/Ubuntu

A package repository is available at https://repo.jellyfin.org. To use it:

0. Install the `dotnet-runtime-2.1` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/dotnet-core/2.1).
0. Import the GPG signing key (signed by Joshua):
    ```
    wget -O - https://repo.jellyfin.org/debian/jellyfin-signing-key-joshua.gpg.key | sudo apt-key add -
    ```
0. Add an entry to `/etc/sources.list.d/jellyfin.list`:
    ```
    echo "deb https://repo.jellyfin.org/debian $( grep -Ewo -m1 --color=none 'jessie|stretch|buster' /etc/os-release || echo buster ) main" | sudo tee /etc/apt/sources.list.d/jellyfin.list
    ```
0. Update APT repositories:
    ```
    sudo apt update
    ```
0. Install Jellyfin:
    ```
    sudo apt install jellyfin
    ```

## Building Jellyfin packages from source

Jellyfin seeks to integrate build facilities for any desired packaging format. Instructions for the various formats can be found below.

NOTE: When building from source, it is strongly advised to clone the full Git repository, rather than using a `.zip`/`.tar` archive.

### Debian/Ubuntu

Debian build facilities are integrated into the repo at `debian/`.

1. Install the `dotnet-sdk-2.1` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/dotnet-core/2.1).
2. Run `dpkg-buildpackage -us -uc -jX`, where X is your core count.
3. Install the resulting `jellyfin*.deb` file on your system.

A huge thanks to Carlos Hernandez who created the Debian build configuration for Emby 3.1.1.
