#!/bin/bash

# Builds the ZIP archive inside the Docker container

set -o errexit
set -o xtrace

# Version variables
NSSM_VERSION="nssm-2.24-101-g897c7ad"
NSSM_URL="http://files.evilt.win/nssm/${NSSM_VERSION}.zip"
FFMPEG_VERSION="ffmpeg-4.2.1-win32-static"
FFMPEG_URL="https://ffmpeg.zeranoe.com/builds/win32/static/${FFMPEG_VERSION}.zip"

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

# Build binary
dotnet publish Jellyfin.Server --configuration Release --self-contained --runtime win-x86 --output /dist/jellyfin_${version}/ "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none;UseAppHost=true"

# Prepare addins
addin_build_dir="$( mktemp -d )"
wget ${NSSM_URL} -O ${addin_build_dir}/nssm.zip
wget ${FFMPEG_URL} -O ${addin_build_dir}/ffmpeg.zip
unzip ${addin_build_dir}/nssm.zip -d ${addin_build_dir}
cp ${addin_build_dir}/${NSSM_VERSION}/win64/nssm.exe /dist/jellyfin_${version}/nssm.exe
unzip ${addin_build_dir}/ffmpeg.zip -d ${addin_build_dir}
cp ${addin_build_dir}/${FFMPEG_VERSION}/bin/ffmpeg.exe /dist/jellyfin_${version}/ffmpeg.exe
cp ${addin_build_dir}/${FFMPEG_VERSION}/bin/ffprobe.exe /dist/jellyfin_${version}/ffprobe.exe
rm -rf ${addin_build_dir}

# Prepare scripts
cp ${SOURCE_DIR}/deployment/windows/legacy/install-jellyfin.ps1 /dist/jellyfin_${version}/install-jellyfin.ps1
cp ${SOURCE_DIR}/deployment/windows/legacy/install.bat /dist/jellyfin_${version}/install.bat

# Create zip package
pushd /dist
zip -r /jellyfin_${version}.portable.zip jellyfin_${version}
popd
rm -rf /dist/jellyfin_${version}

# Move the artifacts out
mkdir -p ${ARTIFACT_DIR}/
mv /jellyfin[-_]*.zip ${ARTIFACT_DIR}/
chown -Rc $(stat -c %u:%g ${ARTIFACT_DIR}) ${ARTIFACT_DIR}
