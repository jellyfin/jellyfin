#!/bin/bash

# Builds the RPM inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

ls -al SOURCES/pkg-src/

# Build RPM
spectool -g -R SPECS/jellyfin.spec
rpmbuild -bs SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"
rpmbuild -bb SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/rpm
mv /root/rpmbuild/RPMS/x86_64/jellyfin-*.rpm /root/rpmbuild/SRPMS/jellyfin-*.src.rpm ${ARTIFACT_DIR}/rpm/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
