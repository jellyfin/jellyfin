#!/usr/bin/env bash

args="${@}"
declare -a docker_envvars
for arg in ${args}; do
    docker_envvars+="-e ${arg} "
done

WORKDIR="$( pwd )"

package_temporary_dir="${WORKDIR}/pkg-dist-tmp"
output_dir="${WORKDIR}/pkg-dist"
current_user="$( whoami )"
image_name="jellyfin-macos-build"

# Determine if sudo should be used for Docker
if [[ ! -z $(id -Gn | grep -q 'docker') ]] \
  && [[ ! ${EUID:-1000} -eq 0 ]] \
  && [[ ! ${USER} == "root" ]] \
  && [[ ! -z $( echo "${OSTYPE}" | grep -q "darwin" ) ]]; then
    docker_sudo="sudo"
else
    docker_sudo=""
fi

# Prepare temporary package dir
mkdir -p "${package_temporary_dir}"
# Set up the build environment Docker image
${docker_sudo} docker build ../.. -t "${image_name}" -f ./Dockerfile
# Build the DEBs and copy out to ${package_temporary_dir}
${docker_sudo} docker run --rm ${docker_envvars} -v "${package_temporary_dir}:/dist" "${image_name}"
# Move the DEBs to the output directory
mkdir -p "${output_dir}"
mv "${package_temporary_dir}"/* "${output_dir}"
