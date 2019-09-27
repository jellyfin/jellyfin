#!/bin/bash

# Builds the DEB inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Remove build-dep for dotnet-sdk-2.2, since it's not a package in this image
sed -i '/dotnet-sdk-2.2,/d' debian/control

# Clone down and build Web frontend
web_build_dir="$( mktemp -d )"
web_target="${SOURCE_DIR}/MediaBrowser.WebDashboard/jellyfin-web"
git clone https://github.com/jellyfin/jellyfin-web.git ${web_build_dir}/
pushd ${web_build_dir}
if [[ -n ${web_branch} ]]; then
    checkout -b origin/${web_branch}
fi
yarn install
mkdir -p ${web_target}
mv dist/* ${web_target}/
popd
rm -rf ${web_build_dir}

# Build DEB
export CONFIG_SITE=/etc/dpkg-cross/cross-config.${ARCH}
dpkg-buildpackage -us -uc -aarm64

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/deb
mv /jellyfin[-_]* ${ARTIFACT_DIR}/deb/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
