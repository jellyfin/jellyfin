#!/usr/bin/env bash

keep_artifacts="${1}"

WORKDIR="$( pwd )"
VERSION="$( grep -A1 '^Version:' ${WORKDIR}/pkg-src/jellyfin.spec | awk '{ print $NF }' )"

package_temporary_dir="${WORKDIR}/pkg-dist-tmp"
package_source_dir="${WORKDIR}/pkg-src"
output_dir="${WORKDIR}/pkg-dist"
current_user="$( whoami )"
image_name="jellyfin-fedora-build"

rm -f "${package_source_dir}/jellyfin-${VERSION}.tar.gz" &>/dev/null \
  || sudo rm -f "${package_source_dir}/jellyfin-${VERSION}.tar.gz" &>/dev/null

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
