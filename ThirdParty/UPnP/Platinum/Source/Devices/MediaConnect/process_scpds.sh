#!/bin/sh

set -x

# abort on any errors
set -e

# ContentDirectory
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v X_MS_MediaReceiverRegistrarSCPD -h MediaConnect X_MS_MediaReceiverRegistrarSCPD.xml X_MS_MediaReceiverRegistrarSCPD.cpp
