#!/bin/bash

# Builds the DEB inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Remove build-dep for dotnet-sdk-3.1, since it's not a package in this image
sed -i '/dotnet-sdk-3.1,/d' debian/control

# Build DEB
dpkg-buildpackage -us -uc

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/deb
mv /jellyfin[-_]* ${ARTIFACT_DIR}/deb/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
