#!/bin/bash

# Builds the DEB inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Build DEB
dpkg-buildpackage -us -uc

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/deb
mv /jellyfin_* ${ARTIFACT_DIR}/deb/
