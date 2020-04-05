#!/bin/bash

# Builds the TAR archive inside the Docker container

set -o errexit
set -o xtrace

# Move to source directory
pushd ${SOURCE_DIR}

# Clone down and build Web frontend
web_build_dir="$( mktemp -d )"
web_target="${SOURCE_DIR}/MediaBrowser.WebDashboard/jellyfin-web"
git clone https://github.com/jellyfin/jellyfin-web.git ${web_build_dir}/
pushd ${web_build_dir}
git checkout tags/v10.5.3
yarn install
mkdir -p ${web_target}
mv dist/* ${web_target}/
popd
rm -rf ${web_build_dir}

# Get version
version="$( grep "version:" ./build.yaml | sed -E 's/version: "([0-9\.]+.*)"/\1/' )"

# Build archives
dotnet publish Jellyfin.Server --configuration Release --output /dist/jellyfin_${version}/ "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none"
tar -cvzf /jellyfin_${version}.portable.tar.gz -C /dist jellyfin_${version}
rm -rf /dist/jellyfin_${version}

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/
mv /jellyfin[-_]*.tar.gz ${ARTIFACT_DIR}/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
