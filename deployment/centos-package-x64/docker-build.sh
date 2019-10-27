#!/bin/bash

# Builds the RPM inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Prepare the source
source "$HOME/.nvm/nvm.sh"
nvm use v8
make -f .copr/Makefile srpm outdir=/root/rpmbuild/SRPMS

# Remove dep for nodejs/yarn since our build env won't have these (NVM instead)
sed -i '/BuildRequires:  nodejs >= 8 yarn/d' SPECS/jellyfin.spec

# Build the RPMs
rpmbuild -bs SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"
rpmbuild -bb SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/rpm
mv /root/rpmbuild/RPMS/x86_64/jellyfin-*.rpm /root/rpmbuild/SRPMS/jellyfin-*.src.rpm ${ARTIFACT_DIR}/rpm/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
