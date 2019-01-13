#!/usr/bin/env bash

source ../common.build.sh

VERSION=`get_version ../..`

# TODO get the version in the package automatically. And using the changelog to decide the debian package suffix version.

# Build a Jellyfin .deb file with Docker on Linux
# Places the output .deb file in the parent directory

package_temporary_dir="`pwd`/pkg-dist-tmp"
output_dir="`pwd`/pkg-dist"
current_user="`whoami`"
image_name="jellyfin-debuild"

cleanup() {
    set +o errexit
    docker image rm $image_name --force
    rm -rf "$package_temporary_dir"
}
trap cleanup EXIT INT

docker build ../.. -t "$image_name" -f ./Dockerfile --build-arg SOURCEDIR="/jellyfin-${VERSION}"
mkdir -p "$package_temporary_dir"
mkdir -p "$output_dir"
docker run --rm -v "$package_temporary_dir:/temp" "$image_name" sh -c 'find / -maxdepth 1 -type f -name "jellyfin*" -exec mv {} /temp \;'
chown -R "$current_user" "$package_temporary_dir" \
|| sudo chown -R "$current_user" "$package_temporary_dir"

mv "$package_temporary_dir"/* "$output_dir"
