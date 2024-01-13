# Jellyfin RPM

## Build Fedora Package with docker

Change into this directory `cd rpm-package`
Run the build script `./build-fedora-rpm.sh`.
Resulting RPM and src.rpm will be in `../../jellyfin-*.rpm`

## ffmpeg

The RPM package for Fedora/CentOS requires some additional repositories as ffmpeg is not in the main repositories.

```shell
# ffmpeg from RPMfusion free
# Fedora
$ sudo dnf install https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
# CentOS 8
$ sudo dnf localinstall --nogpgcheck https://download1.rpmfusion.org/free/el/rpmfusion-free-release-8.noarch.rpm
# CentOS 9
$ sudo dnf localinstall --nogpgcheck https://download1.rpmfusion.org/free/el/rpmfusion-free-release-9.noarch.rpm
```

## Building with dotnet

Jellyfin is build with `--self-contained` so no dotnet required for runtime.

```shell
# dotnet required for building the RPM
# Fedora
$ sudo dnf copr enable @dotnet-sig/dotnet
# CentOS 8
$ sudo rpm -Uvh https://packages.microsoft.com/config/rhel/8/packages-microsoft-prod.rpm
# CentOS 9
$ sudo rpm -Uvh https://packages.microsoft.com/config/rhel/9/packages-microsoft-prod.rpm
```

## TODO

- [ ] OpenSUSE
