# Jellyfin RPM

## Build Fedora Package with docker
We clone our repository `git clone https://github.com/jellyfin/jellyfin`
We go in the folder `cd jellyfin`
Run the build script `./build.sh -t docker -p fedora.amd64 -k`
Resulting RPM and src.rpm will be in `../../jellyfin-*.rpm`

And for the web part 
We clone our repository `git clone https://github.com/jellyfin/jellyfin-web`
Run the build script `./build.sh -t docker -p fedora.amd64 -k`
Resulting RPM and src.rpm will be in `../../jellyfin-*.rpm`

## Build Fedora Package with native host compilation

Go to the root of jellyfin projet
Run the build script `./build.sh -t native -p fedora.amd64`.
Resulting RPM and src.rpm will be in `../../jellyfin-*.rpm`

##  Build dependencies for native host compilation

### ffmpeg dependices for native compilation

The RPM package for Fedora/CentOS requires some additional repositories as ffmpeg is not in the main repositories.

```shell
# ffmpeg from RPMfusion free
# Fedora
$ sudo dnf install https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
# CentOS 7
$ sudo yum localinstall --nogpgcheck https://download1.rpmfusion.org/free/el/rpmfusion-free-release-7.noarch.rpm
```

## ### dotnet dependices for native compilation

Jellyfin is build with `--self-contained` so no dotnet required for runtime.

```shell
# dotnet required for building the RPM
# Fedora
$ sudo dnf copr enable @dotnet-sig/dotnet
# CentOS
$ sudo rpm -Uvh https://packages.microsoft.com/config/rhel/7/packages-microsoft-prod.rpm
```

## TODO

- [ ] OpenSUSE
