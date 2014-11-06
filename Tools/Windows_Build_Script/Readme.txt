Windows Build Script
====================

This document explain howto build MediaBrowser's binaries for redistribution.

Requirements
============
* Windows
* PowerShell 3
* Git
* VS2013 Desktop (for MSBuild)
* Portable Library Tools 2 (http://visualstudiogallery.msdn.microsoft.com/b0e0b5e9-e138-410b-ad10-00cb3caf4981)
* Internet Connection

Portable Library Tools 2 (VS2013 Desktop)
=========================================
Install with the /buildmachine switch:

    PortableLibraryTools.exe /buildmachine

You won't be able to open MediaBrowser.Model.Portable in VS but you will be able to build it.

Build Packages
==============
* Open a PowerShell console.
* Go to Tools\Windows_Build_Scripts
* Run: .\MediaBrowser.Build.ps1

Error and logs
==============
If there's an error the script will stop. Logs are available for each build type in the same folder as the script.

