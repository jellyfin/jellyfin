Linux Build Script
==================

This document explain howto build MediaBrowser's binaries for redistribution.

The two scripts should be together in the same folder.

Requirements
============
* Git
* Tar
* Gzip
* mozroots
* Mono and mono-devel (Tested with Tpokorra release: https://build.opensuse.org/project/show/home:tpokorra:mono)
* xbuild
* Internet Connection

Mkbundle specific requirements
==============================
The OS arch must be the same as the Mono's arch. Example: to build an i686 version you need i686 OS and Mono.


Build Packages(Normal and mkbundle version)
===========================================

Don't forget to set your PATH, LD_LIBRARY_PATH and PKG_CONFIG_PATH, if Mono is not in the standard path.

-Normal
 ------
* Go to the script location via CLI.
* "chmod +x MediaBrowser.Mono.Build.sh", if script is not executable.
* $ ./MediaBrowser.Mono.Build.sh
* Two packages will be available in mediabrowser/:
    * MediaBrowser.Mono.v.x.yyyy.zzzzz.tar.gz
    * MediaBrowser.Mono.mkbundlex.v.x.yyyy.zzzzz.tar.gz

-Mkbundle
 --------
* Execute the Normal version, you will need the mkbundle archive name.
* Go to the script location via CLI.
* "chmod +x MediaBrowser.Mono.Build.mkbundle.sh", if script is not executable.
* $ ./MediaBrowser.Mono.Build.mkbundle.sh mediabrowser/MediaBrowser.Mono.mkbundlex.v.x.yyyy.zzzzz.tar.gz
* The package will be available in mediabrowser/:
    * x64_86: MediaBrowser.Mono.mkbundlex.X86_64.v.x.yyyy.zzzzz.tar.gz
    * i686:   MediaBrowser.Mono.mkbundlex.i686.v.x.yyyy.zzzzz.tar.gz

Error and logs
==============
If there's an error the script will stop. Logs are available for each commands in mediabrowser/logs and mediabrowser/logs_mkbundlex.

