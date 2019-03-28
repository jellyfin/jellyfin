#!/usr/bin/env bash

source ../common.build.sh

keep_artifacts="${1}"

WORKDIR="$( pwd )"

package_temporary_dir="${WORKDIR}/pkg-dist-tmp"
output_dir="${WORKDIR}/pkg-dist"
current_user="$( whoami )"
image_name="jellyfin-ubuntu-build"

rm -rf "${package_temporary_dir}" &>/dev/null \
  || sudo rm -rf "${package_temporary_dir}" &>/dev/null

rm -rf "${output_dir}" &>/dev/null \
  || sudo rm -rf "${output_dir}" &>/dev/null

if [[ ${keep_artifacts} == 'n' ]]; then
    docker_sudo=""
    if [[ ! -z $(id -Gn | grep -q 'docker') ]] \
      && [[ ! ${EUID:-1000} -eq 0 ]] \
      && [[ ! ${USER} == "root" ]] \
      && [[ ! -z $( echo "${OSTYPE}" | grep -q "darwin" ) ]]; then
        docker_sudo=sudo
    fi
    ${docker_sudo} docker image rm ${image_name} --force
fi
