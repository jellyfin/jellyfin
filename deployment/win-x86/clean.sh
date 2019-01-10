#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

clean_jellyfin ../.. Release `pwd`/dist/jellyfin_${VERSION}
