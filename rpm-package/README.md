# unoffical jellyfin RPM

<a href="https://copr.fedorainfracloud.org/coprs/wuerfelbecher/jellyfin/package/jellyfin/"><img src="https://copr.fedorainfracloud.org/coprs/wuerfelbecher/jellyfin/package/jellyfin/status_image/last_build.png" /></a>

## ffmpeg

The RPM package for Fedora/CentOS requires some additional repos as ffmpeg is not in the main repositories.

```shell
# ffmpeg from RPMfusion free
# Fedora
$ sudo dnf install https://download1.rpmfusion.org/free/fedora/rpmfusion-free-release-$(rpm -E %fedora).noarch.rpm
# CentOS 7 
$ sudo yum localinstall --nogpgcheck https://download1.rpmfusion.org/free/el/rpmfusion-free-release-7.noarch.rpm
```

## ISO mounting

To allow jellyfin to mount/umonut ISO files uncomment these two lines in `/etc/sudoers.d/jellyfin-sudoers`
```
# %jellyfin ALL=(ALL) NOPASSWD: /bin/mount
# %jellyfin ALL=(ALL) NOPASSWD: /bin/umount
```


## Database patching
You may need to install sqlite since CentOS has no `Recommends:` with `yum install sqlite`.
To fix the paths in the emby database for a migration to jellyfin run the script:
```shell
/usr/share/jellyfin/update-db-paths.sh <path-to-library.db> <path-to-emby-data> <path-to-jellyfin-data>
```
PS: Please **backup your emby database beforehand**.

## Building with dotnet

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