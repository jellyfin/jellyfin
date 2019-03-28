#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

docker manifest create jellyfin:${VERSION} jellyfin:amd64-${VERSION} jellyfin:arm32v7-${VERSION} jellyfin:arm64v8-${VERSION}
docker manifest annotate jellyfin:amd64-${VERSION} --os linux --arch amd64
#docker manifest annotate jellyfin:arm32v7-${VERSION} --os linux --arch arm --variant armv7
#docker manifest annotate jellyfin:arm64v8-${VERSION} --os linux --arch arm64 --variant armv8

#TODO publish.sh - docker manifest push jellyfin:${VERSION}
