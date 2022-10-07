#!/bin/sh

set -e

dotnet build -c Release ../../Jellyfin.Server/Jellyfin.Server.csproj --output bin
sharpfuzz bin/jellyfin.dll
cp bin/jellyfin.dll .

dotnet build
mkdir -p Findings
AFL_SKIP_BIN_CHECK=1 afl-fuzz -i "Testcases/$1" -o "Findings/$1" -t 5000 -m 10240 dotnet bin/Debug/net6.0/Jellyfin.Server.Fuzz.dll "$1"
