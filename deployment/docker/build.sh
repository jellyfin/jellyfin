#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

build_jellyfin_docker ../.. Dockerfile jellyfin:${VERSION}