#!/usr/bin/env bash

source ../common.build.sh

args="${@}"
declare -a docker_envvars
for arg in ${args}; do
    docker_envvars+=("-e ${arg}")
done

WORKDIR="$( pwd )"
VERSION="$( grep '^Version:' ${WORKDIR}/pkg-src/jellyfin.spec | awk '{ print $NF }' )"

package_temporary_dir="${WORKDIR}/pkg-dist-tmp"
output_dir="${WORKDIR}/pkg-dist"
pkg_src_dir="${WORKDIR}/pkg-src"
current_user="$( whoami )"
image_name="jellyfin-centos-build"

# Determine if sudo should be used for Docker
if [[ ! -z $(id -Gn | grep -q 'docker') ]] \
  && [[ ! ${EUID:-1000} -eq 0 ]] \
  && [[ ! ${USER} == "root" ]] \
  && [[ ! -z $( echo "${OSTYPE}" | grep -q "darwin" ) ]]; then
    docker_sudo="sudo"
else
    docker_sudo=""
fi

# Create RPM source archive
GNU_TAR=1
mkdir -p "${package_temporary_dir}"
echo "Bundling all sources for RPM build."
tar \
--transform "s,^\.,jellyfin-${VERSION}," \
--exclude='.git*' \
--exclude='**/.git' \
--exclude='**/.hg' \
--exclude='**/.vs' \
--exclude='**/.vscode' \
--exclude='deployment' \
--exclude='**/bin' \
--exclude='**/obj' \
--exclude='**/.nuget' \
--exclude='*.deb' \
--exclude='*.rpm' \
-czf "${pkg_src_dir}/jellyfin-${VERSION}.tar.gz" \
-C "../.." ./ || GNU_TAR=0

if [ $GNU_TAR -eq 0 ]; then
    echo "The installed tar binary did not support --transform. Using workaround."
    mkdir -p "${package_temporary_dir}/jellyfin"
    # Not GNU tar
    tar \
    --exclude='.git*' \
    --exclude='**/.git' \
    --exclude='**/.hg' \
    --exclude='**/.vs' \
    --exclude='**/.vscode' \
    --exclude='deployment' \
    --exclude='**/bin' \
    --exclude='**/obj' \
    --exclude='**/.nuget' \
    --exclude='*.deb' \
    --exclude='*.rpm' \
    -zcf \
    "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz" \
    -C "../.." ./
    echo "Extracting filtered package."
    tar -xzf "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz" -C "${package_temporary_dir}/jellyfin-${VERSION}"
    echo "Removing filtered package."
    rm -f "${package_temporary_dir}/jellyfin/jellyfin-${VERSION}.tar.gz"
    echo "Repackaging package into final tarball."
    tar -czf "${pkg_src_dir}/jellyfin-${VERSION}.tar.gz" -C "${package_temporary_dir}" "jellyfin-${VERSION}"
fi

# Set up the build environment Docker image
${docker_sudo} docker build ../.. -t "${image_name}" -f ./Dockerfile
# Build the RPMs and copy out to ${package_temporary_dir}
${docker_sudo} docker run --rm -v "${package_temporary_dir}:/dist" "${image_name}" ${docker_envvars}
# Move the RPMs to the output directory
mkdir -p "${output_dir}"
mv "${package_temporary_dir}"/rpm/* "${output_dir}"
