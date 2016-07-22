Linux Build Script
==================

This document explain howto build MediaBrowser's binaries for redistribution.

The two scripts should be together in the same folder.

Requirements
============
* Internet Connection
* git
* tar
* gzip
* mozroots
* mono v4.2.3.4 and mono-devel
* xbuild

Upstream mono installation guide: http://www.mono-project.com/docs/getting-started/install/linux/

Debian-based systems
====================
You can use a specific snapshot of mono with an APT source like:
```
deb http://download.mono-project.com/repo/debian wheezy/snapshots/4.2.3.4 main
```

Packages needed:
```
$ apt-get install mono-runtime mono-xbuild mono-utils mono-devel libmono-system-core4.0-cil
```

Mkbundle specific requirements
==============================
The OS arch must be the same as the Mono's arch. Example: to build an i686 version you need i686 OS and Mono.


Build Packages (Normal and mkbundle version)
===========================================

Don't forget to set your PATH, LD_LIBRARY_PATH and PKG_CONFIG_PATH, if Mono is not in the standard path.

-Normal
 ------
* `$ cd Tools/Linux_Build_Scripts`
* `$ ./MediaBrowser.Mono.Build.sh`
* Two packages will be available in mediabrowser/:
    * MediaBrowser.Mono.v.x.yyyy.zzzzz.tar.gz
    * MediaBrowser.Mono.mkbundlex.v.x.yyyy.zzzzz.tar.gz

-Mkbundle
 --------
* Execute the Normal version, you will need the mkbundle archive name.
* Go to the script location via CLI.
* $ ./MediaBrowser.Mono.Build.mkbundle.sh mediabrowser/MediaBrowser.Mono.mkbundlex.v.x.yyyy.zzzzz.tar.gz
* The package will be available in mediabrowser/:
    * x64_86: MediaBrowser.Mono.mkbundlex.X86_64.v.x.yyyy.zzzzz.tar.gz
    * i686:   MediaBrowser.Mono.mkbundlex.i686.v.x.yyyy.zzzzz.tar.gz
