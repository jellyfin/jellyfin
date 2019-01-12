#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

build_jellyfin ../../Jellyfin.Server Release win-x86 `pwd`/dist/jellyfin_${VERSION}
