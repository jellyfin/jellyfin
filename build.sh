#!/usr/bin/env bash

# build.sh - Build Jellyfin binary packages
# Part of the Jellyfin Project

set -o errexit
set -o pipefail

usage() {
    echo -e "build.sh - Build Jellyfin binary packages"
    echo -e "Usage:"
    echo -e "  $0 -t/--type <BUILD_TYPE> -p/--platform <PLATFORM> [-k/--keep-artifacts] [-l/--list-platforms]"
    echo -e "Notes:"
    echo -e "  * BUILD_TYPE can be one of: [native, docker] and must be specified"
    echo -e "    * native: Build using the build script in the host OS"
    echo -e "    * docker: Build using the build script in a standardized Docker container"
    echo -e "  * PLATFORM can be any platform shown by -l/--list-platforms and must be specified"
    echo -e "  * If -k/--keep-artifacts is specified, transient artifacts (e.g. Docker containers) will be"
    echo -e "    retained after the build is finished; the source directory will still be cleaned"
    echo -e "  * If -l/--list-platforms is specified, all other arguments are ignored; the script will print"
    echo -e "    the list of supported platforms and exit"
}

list_platforms() {
    declare -a platforms
    platforms=(
        $( find deployment -maxdepth 1 -mindepth 1 -name "build.*" | awk -F'.' '{ $1=""; printf $2; if ($3 != ""){ printf "." $3; }; if ($4 != ""){ printf "." $4; };  print ""; }' | sort )
    )
    echo -e "Valid platforms:"
    echo
    for platform in ${platforms[@]}; do
        echo -e "* ${platform} : $( grep '^#=' deployment/build.${platform} | sed 's/^#= //' )"
    done
}

do_build_native() {
    if [[ ! -f $( which dpkg ) || $( dpkg --print-architecture | head -1 ) != "${PLATFORM##*.}" ]]; then
        echo "Cross-building is not supported for native builds, use 'docker' builds on amd64 for cross-building."
        exit 1
    fi
    export IS_DOCKER=NO
    deployment/build.${PLATFORM}
}

do_build_docker() {
    if [[ -f $( which dpkg ) && $( dpkg --print-architecture | head -1 ) != "amd64" ]]; then
        echo "Docker-based builds only support amd64-based cross-building; use a 'native' build instead."
        exit 1
    fi
    if [[ ! -f deployment/Dockerfile.${PLATFORM} ]]; then
        echo "Missing Dockerfile for platform ${PLATFORM}"
        exit 1
    fi
    if [[ ${KEEP_ARTIFACTS} == YES ]]; then
        docker_args=""
    else
        docker_args="--rm"
    fi

    docker build . -t "jellyfin-builder.${PLATFORM}" -f deployment/Dockerfile.${PLATFORM}
    mkdir -p ${ARTIFACT_DIR}
    docker run $docker_args -v "${SOURCE_DIR}:/jellyfin" -v "${ARTIFACT_DIR}:/dist" "jellyfin-builder.${PLATFORM}"
}

while [[ $# -gt 0 ]]; do
    key="$1"
    case $key in
        -t|--type)
        BUILD_TYPE="$2"
        shift # past argument
        shift # past value
        ;;
        -p|--platform)
        PLATFORM="$2"
        shift # past argument
        shift # past value
        ;;
        -k|--keep-artifacts)
        KEEP_ARTIFACTS=YES
        shift # past argument
        ;;
        -l|--list-platforms)
        list_platforms
        exit 0
        ;;
        -h|--help)
        usage
        exit 0
        ;;
        *)    # unknown option
        echo "Unknown option $1"
        usage
        exit 1
        ;;
    esac
done

if [[ -z ${BUILD_TYPE} || -z ${PLATFORM} ]]; then
    usage
    exit 1
fi

export SOURCE_DIR="$( pwd )"
export ARTIFACT_DIR="${SOURCE_DIR}/../bin/${PLATFORM}"

# Determine build type
case ${BUILD_TYPE} in
    native)
        do_build_native
    ;;
    docker)
        do_build_docker
    ;;
esac
