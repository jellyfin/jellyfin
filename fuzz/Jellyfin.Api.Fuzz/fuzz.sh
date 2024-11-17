#!/bin/sh

set -e

dotnet build -c Release ../../Jellyfin.Api/Jellyfin.Api.csproj --output bin
sharpfuzz bin/Jellyfin.Api.dll
cp bin/Jellyfin.Api.dll .

dotnet build
mkdir -p Findings
AFL_SKIP_BIN_CHECK=1 afl-fuzz -i "Testcases/$1" -o "Findings/$1" -t 5000 ./bin/Debug/net9.0/Jellyfin.Api.Fuzz "$1"
