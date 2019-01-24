#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

package_temporary_dir="`pwd`/pkg-dist-tmp"
pkg_src_dir="`pwd`/pkg-src"
image_name="jellyfin-rpmbuild"
docker_sudo=""
if ! $(id -Gn | grep -q 'docker') && [ ! ${EUID:-1000} -eq 0 ] && [ ! $USER == "root" ]; then
    docker_sudo=sudo
fi

$docker_sudo docker image rm $image_name --force
rm -rf "$package_temporary_dir"
rm -rf "$pkg_src_dir/jellyfin-${VERSION}.tar.gz"
