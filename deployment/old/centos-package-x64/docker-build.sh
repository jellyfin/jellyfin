#!/bin/bash

# Builds the RPM inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Build RPM
make -f .copr/Makefile srpm outdir=/root/rpmbuild/SRPMS
rpmbuild --rebuild -bb /root/rpmbuild/SRPMS/jellyfin-*.src.rpm

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/rpm
mv /root/rpmbuild/RPMS/x86_64/jellyfin-*.rpm /root/rpmbuild/SRPMS/jellyfin-*.src.rpm ${ARTIFACT_DIR}/rpm/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
