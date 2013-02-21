Platinum UPnP SDK
=================

This toolkit consists of 2 modules:
* Neptune : a C++ Runtime Library
* Platinum: a modular UPnP Framework [Platinum uses Neptune]

Unless you intend to use Neptune independently from Platinum, it is recommended that you build binaries directly from the "Build" tree of Platinum. All the dependent binaries will be rebuilt automatically (including Neptune).

---------------------------------------------
BUILDING SDK & SAMPLE APPLICATIONS

* Windows:
Open the Visual Studio 2008 solution located @ Platinum\Build\Targets\x86-microsoft-win32-vs2008\Platinum.sln

* MacOSX, iOS:
Open the XCode project file located @ Platinum/Build/Targets/universal-apple-macosx/Platinum.xcodeproj
To include Platinum to your XCode projects, simply add the project file then add Platinum as a Target Dependency as well as libPlatinum.lib in Link Binaries.
Alternatively, you can build the Platinum.Framework using the PlatinumFramework target and add it to your project.

* Linux, Cygwin, MacOSX, iOS
Open a shell, go to the Platinum root directory and type 'scons' (http://scons.org). 
The output of the scons build will be found under Platinum/Build/Targets/{TARGET}/{Debug|Release}. 
Additionally, the output is copied under Platinum/Targets/{TARGET}/{Debug|Release} for convenience when applicable.

Command Line Examples:
Builds libPlatinum.a, Platinum.Framework on both OSX & iOS using Xcode. Apps & Tests on OSX only.
[The framework for OSX is i386 and x86_64 compatible, the iOS version is armv6, armv7 and i386 compatible so you can link with it and run your app on device and simulator]
> scons target=universal-apple-macosx-xcode build_config=Release

Builds Platinum.lib, Tests & Apps for Windows
> scons target=x86-microsoft-win32 build_config=Release

---------------------------------------------
RUNNING SAMPLE APPLICATIONS

* FileMediaServerTest
---------------------
This is an example of a UPnP MediaServer. Given a path, it allows a UPnP ControlPoint to browse the content of the directory and its sub-directories. Additionally, files can be streamed (Note that only files with known mimetypes are advertised).

usage: FileMediaServerTest [-f <friendly_name>] <path>
    -f : optional upnp server friendly name
    <path> : local path to serve

Once started, type 'q' to quit.

* MediaRendererTest
-------------------
This is an example shell of a UPnP MediaRenderer. It is to be contolled by a UPnP ControlPoint. This is just a SHELL, this won't play anything yet. You need to hook up the playback functionality yourself.

usage: MediaRendererTest [-f <friendly_name>]
    -f : optional upnp server friendly name

Once started, type 'q' to quit.

* MediaCrawler
--------------
This is a combo UPnP MediaServer + ControlPoint. It browses content from other MediaServers it finds on the network and present them under one single aggregated view. This is useful for some devices that need to select one single MediaServer at boot time (i.e. Roku).

Once started, type 'q' to quit.

* MicroMediaController
----------------------
This is a ControlPoint (synchronous) that lets you browse any MediaServer using a shell-like interface. Once started, a command prompt lets you enter commands such as:
     quit    -   shutdown
     exit    -   same as quit
     setms   -   select a media server to become the active media server
     getms   -   print the friendly name of the active media server
     ls      -   list the contents of the current directory on the active 
                 media server
     cd      -   traverse down one level in the content tree on the active
                 media server
     cd ..   -   traverse up one level in the content tree on the active
                 media server
     pwd     -   print the path from the root to your current position in the 
                 content tree on the active media server
                 
Experimental MediaRenderer commands (not yet full implemented):
     setmr   -   select a media renderer to become the active media renderer
     getmr   -   print the friendly name of the active media renderer
     open    -   set the uri on the active media renderer
     play    -   play the active uri on the active media renderer
     stop    -   stop the active uri on the active media renderer
     
* MediaConnect
--------------
This is a derived implementation of the FileMediaServerTest with the only difference that it makes it visible to a XBox 360.

* MediaServerCocoaTest
----------------------
A basic cocoa test server app showing how to use the Platinum framework on Mac OSX.

---------------------------------------------
LANGUAGE BINDINGS

* Objective-C
-------------
Under Source/Extras/ObjectiveC

* C++/CLR
---------
Under Source/Extras/Managed

* Android Java/JNI
------------------
To build the JNI shared library, you will need to have installed the Android NDK and set up the proper environment variables such as ANDROID_NDK_ROOT. 
> cd <PlatinumKit>/Platinum
> scons target=arm-android-linux build_config=Release

> cd <PlatinumKit>/Platinum/Source/Platform/Android/modules/platinum/jni
> ndk-build NDK_DEBUG=0

> import eclipse Android .project located @ <PlatinumKit>/Platinum/Source/Platform/Android/modules/platinum/
This will create the jar file @ <PlatinumKit>/Platinum/Source/Platform/Android/modules/platinum/bin/platinum.jar

> To Test the Platinum jni layer, import into eclipse both Android projects located @ <PlatinumKit>/Platinum/Source/Platform/Android/samples/sample-upnp & <PlatinumKit>/Platinum/Source/Platform/Android/modules/platinum.

