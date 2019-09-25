#!/usr/bin/env bash
set -x
package_win64() (
    local NSSM_VERSION="nssm-2.24-101-g897c7ad"
    local NSSM_URL="https://nssm.cc/ci/${NSSM_VERSION}.zip"
    local FFMPEG_VERSION="ffmpeg-4.0.2-win64-static"
    local FFMPEG_URL="https://ffmpeg.zeranoe.com/builds/win64/static/${FFMPEG_VERSION}.zip"
    local ROOT=${1-$DEFAULT_ROOT}
    local OUTPUT_DIR=${2-$DEFAULT_OUTPUT_DIR}
    local PKG_DIR=${3-$DEFAULT_PKG_DIR}
    local ARCHIVE_CMD="zip -r"
    # Package portable build result
    if [ -d ${OUTPUT_DIR} ]; then      
        echo -e "${CYAN}Packaging build in '${OUTPUT_DIR}' for `basename "${OUTPUT_DIR}"` to '${PKG_DIR}' with root '${ROOT}'.${NC}"
        local TEMP_DIR="$(mktemp -d)" 
        wget ${NSSM_URL} -O ${TEMP_DIR}/nssm.zip
        wget ${FFMPEG_URL} -O ${TEMP_DIR}/ffmpeg.zip
        unzip ${TEMP_DIR}/nssm.zip -d $TEMP_DIR
        cp ${TEMP_DIR}/${NSSM_VERSION}/win64/nssm.exe ${OUTPUT_DIR}/nssm.exe
        unzip ${TEMP_DIR}/ffmpeg.zip -d $TEMP_DIR
        cp ${TEMP_DIR}/${FFMPEG_VERSION}/bin/ffmpeg.exe ${OUTPUT_DIR}/ffmpeg.exe
        cp ${TEMP_DIR}/${FFMPEG_VERSION}/bin/ffprobe.exe ${OUTPUT_DIR}/ffprobe.exe
        rm -r ${TEMP_DIR}
        cp ${ROOT}/deployment/windows/legacy/install-jellyfin.ps1 ${OUTPUT_DIR}/install-jellyfin.ps1
        cp ${ROOT}/deployment/windows/legacy/install.bat ${OUTPUT_DIR}/install.bat
        mkdir -p ${PKG_DIR}
        pushd ${OUTPUT_DIR} 
        ${ARCHIVE_CMD} ${ROOT}/${PKG_DIR}/`basename "${OUTPUT_DIR}"`.zip .
        popd
        local EXIT_CODE=$?
        if [ $EXIT_CODE -eq 0 ]; then
            echo -e "${GREEN}[DONE] Packaging build in '${OUTPUT_DIR}' for `basename "${OUTPUT_DIR}"` to '${PKG_DIR}' with root '${ROOT}' complete.${NC}"
        else
            echo -e "${RED}[FAIL] Packaging build in '${OUTPUT_DIR}' for `basename "${OUTPUT_DIR}"` to '${PKG_DIR}' with root '${ROOT}' FAILED.${NC}"
        fi       
    else
        echo -e "${RED}[FAIL] Build artifacts do not exist for ${OUTPUT_DIR}. Run build.sh first.${NC}"
    fi
)

source ../common.build.sh

VERSION=`get_version ../..`

package_win64 ../.. `pwd`/dist/jellyfin_${VERSION}

#TODO setup and maybe change above code to produce the Windows native zip format.
