#!/bin/sh

set -x

# abort on any errors
set -e

# ContentDirectory
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v MS_ContentDirectorySCPD -h ContentDirectory ContentDirectorySCPD.xml ContentDirectorySCPD.cpp

# ContentDirectory with Search
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v MS_ContentDirectorywSearchSCPD -h ContentDirectory ContentDirectorywSearchSCPD.xml ContentDirectorywSearchSCPD.cpp

# ConnectionManager
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v MS_ConnectionManagerSCPD -h ConnectionManager ConnectionManagerSCPD.xml ConnectionManagerSCPD.cpp