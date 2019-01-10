#!/usr/bin/env bash

source common.build.sh

VERSION=`get_version ..`

COLLECT_DIR="./collect-dist"

mkdir -p ./collect-dist

DIRS=`find . -type d -name "pkg-dist"`

while read directory
do
    echo "Collecting everything from '$directory'.."
    PLATFORM=$(basename "$(dirname "$directory")")
    # Copy all artifacts with extensions tar.gz, deb, exe, zip, rpm and add the platform name to resolve any duplicates.
    find $directory \( -name "jellyfin*.tar.gz" -o -name "jellyfin*.deb" -o -name "jellyfin*.rpm" -o -name "jellyfin*.zip" -o -name "jellyfin*.exe" \) -exec sh -c 'cp "$1" "'${COLLECT_DIR}'/jellyfin_'${PLATFORM}'_${1#*jellyfin_}"' _ {} \;

done <<< "${DIRS}"
