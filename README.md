Jellyfin
============

Jellyfin is a personal media server. The Jellyfin project was started as a result of Emby's decision to take their code closed-source, as well as various philosophical differences with the core developers. Jellyfin seeks to be the free software alternative to Emby and Plex to provide media management and streaming from a dedicated server to end-user devices.

Jellyfin is descended from Emby 3.5.2, ported to the .NET Core framework, and aims to contain build facilities for every platform.

For further details, please see [our wiki](https://github.com/jellyfin/jellyfin/wiki). To receive the latest project updates feel free to join [our public chat on Matrix/Riot](https://matrix.to/#/#jellyfin:matrix.org) and to subscribe to [our subreddit](https://www.reddit.com/r/jellyfin/).

## Feature Requests

While our first priority is a stable build, we will eventually add features that were missing in Emby or were not well implemented (technically or philosophically).

[Feature Requests](http://feathub.com/jellyfin/jellyfin)

## Contributing to Jellyfin

If you're interested in contributing, please see [CONTRIBUTING.md](https://github.com/jellyfin/jellyfin/blob/master/CONTRIBUTING.md).

## Prebuilt Jellyfin packages

Prebuild packages are available for Debian/Ubuntu and Arch, and via Docker Hub.

### Docker

The Jellyfin Docker image is available on Docker Hub at https://hub.docker.com/r/jellyfin/jellyfin/

### Arch

The Jellyfin package is in the AUR at https://aur.archlinux.org/packages/jellyfin-git/

### Unraid

An Unraid Docker template is available. See [this documentation page](https://github.com/jellyfin/jellyfin/blob/master/unRaid/docker-templates/README.md) for details on installing it.

### Debian/Ubuntu

A package repository is available at https://repo.jellyfin.org.

#### Clean install

0. Install the `dotnet-runtime-2.2` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/dotnet-core/2.2).
0. Import the GPG signing key (signed by Joshua):
    ```
    wget -O - https://repo.jellyfin.org/debian/jellyfin-signing-key-joshua.gpg.key | sudo apt-key add -
    ```
0. Add an entry to `/etc/apt/sources.list.d/jellyfin.list` (note that Ubuntu will get `buster` in the list file, but this should still work fine):
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

#### Upgrade from Emby

The following procedure should work to upgrade from Emby to Jellyfin on an existing installation:

0. Upgrade to Emby 3.5.X, preferably 3.5.2, so the database schema is fully up-to-date and consistent. This is somewhat optional but can reduce the risk of obscure bugs later on.
0. Stop the `emby-server` daemon:
    ```
    sudo service emby-server stop
    ```
0. Move your existing Emby data directory out of the way:
    ```
    sudo mv /var/lib/emby /var/lib/emby.backup
    ```
0. Remove the `emby-server` package:
    ```
    sudo apt remove emby-server
    ```
0. Install the `jellyfin` package using the instructions above, verifying that `/var/lib/emby` is a symlink to `/var/lib/jellyfin`.
0. Stop the `jellyfin` daemon:
    ```
    sudo service jellyfin stop
    ```
0. Copy over all the data files from the old backup data directory:
    ```
    sudo cp -a /var/lib/emby.backup/* /var/lib/jellyfin/
    ```
0. Correct ownership on the new data directory:
    ```
    sudo chown -R jellyfin:jellyfin /var/lib/jellyfin
    ```
0. Start the `jellyfin` daemon:
    ```
    sudo service jellyfin start
    ```

This same general procedure should apply for upgrades on any other platform as well though the specific commands and/or paths may be different.

## Building Jellyfin packages from source

Jellyfin seeks to integrate build facilities for any desired packaging format. Instructions for the various formats can be found below.

NOTE: When building from source, only cloning the full Git repository is supported, rather than using a `.zip`/`.tar` archive, in order to support submodules.

### Debian/Ubuntu

Debian build facilities are integrated into the repo at `debian/`.

0. Install the `dotnet-sdk-2.2` package via [Microsoft's repositories](https://dotnet.microsoft.com/download/dotnet-core/2.2).
0. Run `dpkg-buildpackage -us -uc`.
0. Install the resulting `jellyfin_*.deb` file on your system.

A huge thanks to Carlos Hernandez who created the original Debian build configuration for Emby 3.1.1.

### Windows (64 bit)

A pre-built windows installer will be available soon. Until then it isn't too hard to install Jellyfin from Source.

0. Install the dotnet core SDK 2.2 from [Microsoft's Webpage](https://dotnet.microsoft.com/download/dotnet-core/2.2) and [install Git for Windows](https://gitforwindows.org/)
0. Clone Jellyfin into a directory of your choice.
    ```
    git clone https://github.com/jellyfin/jellyfin.git C:\Jellyfin
    ```
0. From the Jellyfin directory you can use our Jellyfin build script. Call `Build-Jellyfin.ps1 -InstallFFMPEG` from inside the directory in a powershell window. Make sure you've set your executionpolicy to unsrestricted.

    Additional flags:
      * If you want to optimize for your environment you can use the `-WindowsVersion` and `-Architecture` flags to do so; the default is generic Windows x64.
      * The `-InstallLocation` flag lets you select where the compiled binaries go; the default is `$Env:AppData\JellyFin-Server\` .
      * The `-InstallFFMPEG` flag will automatically pull the stable ffmpeg binaries appropriate to your architecture (x86/x64 only for now) from [Zeranoe](https://ffmpeg.zeranoe.com/builds/) and place them in your Jellyfin directory. 
0. (Optional) Use [NSSM](https://nssm.cc/) to configure JellyFin to run as a service
0. Jellyfin is now available in the default directory (or the directory you chose). Assuming you kept the default directory, to start it from a Powershell window, run, `&"$env:APPDATA\Jellyfin-Server\EmbyServer.exe"`. To start it from CMD, run, `%APPDATA%\Jellyfin-Server\EmbyServer.exe`
