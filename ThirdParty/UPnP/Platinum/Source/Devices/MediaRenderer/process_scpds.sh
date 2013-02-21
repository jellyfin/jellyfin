#!/bin/sh

set -x

# abort on any errors
set -e

# AVTransport
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v RDR_AVTransportSCPD -h AVTransport AVTransportSCPD.xml AVTransportSCPD.cpp

# Rendering Control
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v RDR_RenderingControlSCPD -h RenderingControl RenderingControlSCPD.xml RenderingControlSCPD.cpp

# ConnectionManager
../../../Targets/x86-unknown-cygwin/Debug/TextToHeader.exe -v RDR_ConnectionManagerSCPD -h ConnectionManager ConnectionManagerSCPD.xml RdrConnectionManagerSCPD.cpp