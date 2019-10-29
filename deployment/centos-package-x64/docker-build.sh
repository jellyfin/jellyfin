#!/bin/bash

# Builds the RPM inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

VERSION="$( grep '^Version:' ${SOURCE_DIR}/SOURCES/pkg-src/jellyfin.spec | awk '{ print $NF }' )"

# Clone down and build Web frontend
web_build_dir="$( mktemp -d )"
web_target="${SOURCE_DIR}/MediaBrowser.WebDashboard/jellyfin-web"
git clone https://github.com/jellyfin/jellyfin-web.git ${web_build_dir}/
pushd ${web_build_dir}
if [[ -n ${web_branch} ]]; then
    checkout -b origin/${web_branch}
fi
source "$HOME/.nvm/nvm.sh"
nvm use v8
yarn install
mkdir -p ${web_target}
mv dist/* ${web_target}/
popd
rm -rf ${web_build_dir}

# Create RPM source archive
GNU_TAR=1
echo "Bundling all sources for RPM build."
tar \
--transform "s,^\.,jellyfin-${VERSION}," \
--exclude='.git*' \
--exclude='**/.git' \
--exclude='**/.hg' \
--exclude='**/.vs' \
--exclude='**/.vscode' \
--exclude='deployment' \
--exclude='**/bin' \
--exclude='**/obj' \
--exclude='**/.nuget' \
--exclude='*.deb' \
--exclude='*.rpm' \
-czf "${SOURCE_DIR}/SOURCES/pkg-src/jellyfin-${VERSION}.tar.gz" \
-C ${SOURCE_DIR} ./ || GNU_TAR=0

if [ $GNU_TAR -eq 0 ]; then
    echo "The installed tar binary did not support --transform. Using workaround."
    package_temporary_dir="$( mktemp -d )"
    mkdir -p "${package_temporary_dir}/jellyfin"
    # Not GNU tar
    tar \
    --exclude='.git*' \
    --exclude='**/.git' \
    --exclude='**/.hg' \
    --exclude='**/.vs' \
    --exclude='**/.vscode' \
    --exclude='deployment' \
    --exclude='**/bin' \
    --exclude='**/obj' \
    --exclude='**/.nuget' \
    --exclude='*.deb' \
    --exclude='*.rpm' \
    -czf "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz" \
    -C ${SOURCE_DIR} ./
    echo "Extracting filtered package."
    mkdir -p "${package_temporary_dir}/jellyfin-${VERSION}"
    tar -xzf "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz" -C "${package_temporary_dir}/jellyfin-${VERSION}"
    echo "Removing filtered package."
    rm -f "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz"
    echo "Repackaging package into final tarball."
    tar -czf "${SOURCE_DIR}/SOURCES/pkg-src/jellyfin-${VERSION}.tar.gz" -C "${package_temporary_dir}" "jellyfin-${VERSION}"
    rm -rf ${package_temporary_dir}
fi

# Build RPM
spectool -g -R SPECS/jellyfin.spec
rpmbuild -bs SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"
rpmbuild -bb SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/rpm
mv /root/rpmbuild/RPMS/x86_64/jellyfin-*.rpm /root/rpmbuild/SRPMS/jellyfin-*.src.rpm ${ARTIFACT_DIR}/rpm/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
