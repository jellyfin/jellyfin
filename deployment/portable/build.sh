#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

#Magic word framework will create a non self contained build
build_jellyfin ../../Jellyfin.Server Release framework `pwd`/dist/jellyfin_${VERSION}
