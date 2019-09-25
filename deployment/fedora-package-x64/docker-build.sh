#!/bin/bash

# Builds the RPM inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Clone down and build Web frontend
web_build_dir="$( mktemp -d )"
web_target="${SOURCE_DIR}/MediaBrowser.WebDashboard/jellyfin-web"
git clone https://github.com/jellyfin/jellyfin-web.git ${web_build_dir}/
pushd ${web_build_dir}
if [[ -n ${web_branch} ]]; then
    checkout -b origin/${web_branch}
fi
yarn install
yarn build
mkdir -p ${web_target}
mv dist/* ${web_target}/
popd
rm -rf ${web_build_dir}

# Build RPM
spectool -g -R SPECS/jellyfin.spec
rpmbuild -bs SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"
rpmbuild -bb SPECS/jellyfin.spec --define "_sourcedir ${SOURCE_DIR}/SOURCES/pkg-src/"

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/rpm
mv /root/rpmbuild/RPMS/x86_64/jellyfin-*.rpm /root/rpmbuild/SRPMS/jellyfin-*.src.rpm ${ARTIFACT_DIR}/rpm/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
