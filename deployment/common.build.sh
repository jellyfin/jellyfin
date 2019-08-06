#!/usr/bin/env bash

set -o errexit
set -o nounset

RED='\033[0;31m'
GREEN='\033[0;32m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

DEFAULT_BUILD_CONTEXT="../.."
DEFAULT_ROOT="."
DEFAULT_DOTNETRUNTIME="framework"
DEFAULT_CONFIG="Release"
DEFAULT_OUTPUT_DIR="dist/jellyfin-git"
DEFAULT_PKG_DIR="pkg-dist"
DEFAULT_DOCKERFILE="Dockerfile"
DEFAULT_ARCHIVE_CMD="tar -xvzf"

# Parse the version from the build.yaml version
get_version()
(
    local ROOT=${1-$DEFAULT_ROOT}
    grep "version:" ${ROOT}/build.yaml \
        | sed -E 's/version: "([0-9\.]+.*)"/\1/' 
)

# Run a build
build_jellyfin()
(
    ROOT=${1-$DEFAULT_ROOT}
    CONFIG=${2-$DEFAULT_CONFIG}
    DOTNETRUNTIME=${3-$DEFAULT_DOTNETRUNTIME}
    OUTPUT_DIR=${4-$DEFAULT_OUTPUT_DIR}

    echo -e "${CYAN}Building jellyfin in '${ROOT}' for ${DOTNETRUNTIME} with configuration ${CONFIG} and output directory '${OUTPUT_DIR}'.${NC}"
    if [[ $DOTNETRUNTIME == 'framework' ]]; then
        dotnet publish "${ROOT}" --configuration "${CONFIG}" --output="${OUTPUT_DIR}" "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none"
    else
        dotnet publish "${ROOT}" --configuration "${CONFIG}" --output="${OUTPUT_DIR}" --self-contained --runtime ${DOTNETRUNTIME} "-p:GenerateDocumentationFile=false;DebugSymbols=false;DebugType=none"
    fi    
    EXIT_CODE=$?
    if [ $EXIT_CODE -eq 0 ]; then
        echo -e "${GREEN}[DONE] Build jellyfin in '${ROOT}' for ${DOTNETRUNTIME} with configuration ${CONFIG} and output directory '${OUTPUT_DIR}' complete.${NC}"
    else
        echo -e "${RED}[FAIL] Build jellyfin in '${ROOT}' for ${DOTNETRUNTIME} with configuration ${CONFIG} and output directory '${OUTPUT_DIR}' FAILED.${NC}"
    fi    
)

# Run a docker
build_jellyfin_docker()
(
    BUILD_CONTEXT=${1-$DEFAULT_BUILD_CONTEXT}
    DOCKERFILE=${2-$DEFAULT_DOCKERFILE}
    IMAGE_TAG=${3-"jellyfin:$(git rev-parse --abbrev-ref HEAD)"}

    echo -e "${CYAN}Building jellyfin docker image in '${BUILD_CONTEXT}' with Dockerfile '${DOCKERFILE}' and tag '${IMAGE_TAG}'.${NC}"
    docker build -t ${IMAGE_TAG} -f ${DOCKERFILE} ${BUILD_CONTEXT}
    EXIT_CODE=$?
    if [ $EXIT_CODE -eq 0 ]; then
        echo -e "${GREEN}[DONE] Building jellyfin docker image in '${BUILD_CONTEXT}' with Dockerfile '${DOCKERFILE}' and tag '${IMAGE_TAG}' complete.${NC}"
    else
        echo -e "${RED}[FAIL] Building jellyfin docker image in '${BUILD_CONTEXT}' with Dockerfile '${DOCKERFILE}' and tag '${IMAGE_TAG}' FAILED.${NC}"
    fi    
)

# Clean a build
clean_jellyfin()
(
    local ROOT=${1-$DEFAULT_ROOT}
    local CONFIG=${2-$DEFAULT_CONFIG}
    local OUTPUT_DIR=${3-$DEFAULT_OUTPUT_DIR}
    local PKG_DIR=${4-$DEFAULT_PKG_DIR}
    echo -e "${CYAN}Cleaning jellyfin in '${ROOT}'' with configuration ${CONFIG} and output directory '${OUTPUT_DIR}'.${NC}"
    echo -e "${CYAN}Deleting '${OUTPUT_DIR}'${NC}"
    rm -rf "$OUTPUT_DIR"
    echo -e "${CYAN}Deleting '${PKG_DIR}'${NC}"
    rm -rf "$PKG_DIR"
    dotnet clean "${ROOT}" -maxcpucount:1 --configuration ${CONFIG}
    local EXIT_CODE=$?
    if [ $EXIT_CODE -eq 0 ]; then
        echo -e "${GREEN}[DONE] Clean jellyfin in '${ROOT}' with configuration ${CONFIG} and output directory '${OUTPUT_DIR}' complete.${NC}"
    else
        echo -e "${RED}[FAIL] Clean jellyfin in '${ROOT}' with configuration ${CONFIG} and output directory '${OUTPUT_DIR}' failed.${NC}"
    fi
)

# Packages the output folder into an archive.
package_portable()
(
    local ROOT=${1-$DEFAULT_ROOT}
    local OUTPUT_DIR=${2-$DEFAULT_OUTPUT_DIR}
    local PKG_DIR=${3-$DEFAULT_PKG_DIR}
    local ARCHIVE_CMD=${4-$DEFAULT_ARCHIVE_CMD}
    # Package portable build result
    if [ -d ${OUTPUT_DIR} ]; then        
        echo -e "${CYAN}Packaging build in '${OUTPUT_DIR}' for `basename "${OUTPUT_DIR}"` to '${PKG_DIR}' with root '${ROOT}'.${NC}"
        mkdir -p ${PKG_DIR}
        tar -zcvf "${PKG_DIR}/`basename "${OUTPUT_DIR}"`.portable.tar.gz" -C "`dirname "${OUTPUT_DIR}"`" "`basename "${OUTPUT_DIR}"`"
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

