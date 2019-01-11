#!/usr/bin/env sh

source ../common.build.sh

VERSION=`get_version ../..`

# TODO get the version in the package automatically. And using the changelog to decide the debian package suffix version.

# Build a Jellyfin .rpm file with Docker on Linux
# Places the output .rpm file in the parent directory

set -o errexit
set -o xtrace
set -o nounset

package_temporary_dir="`pwd`/pkg-dist-tmp"
output_dir="`pwd`/pkg-dist"
current_user="`whoami`"
image_name="jellyfin-rpmbuild"

cleanup() {
    set +o errexit
    docker image rm $image_name --force
    rm -rf "$package_temporary_dir"
}
trap cleanup EXIT INT

docker build ../.. -t "$image_name" -f ./Dockerfile.fedora_package
mkdir -p "$package_temporary_dir"
mkdir -p "$output_dir"
docker run --rm -v "$package_temporary_dir:/temp" "$image_name" sh -c 'find / -maxdepth 1 -type f -name "jellyfin*" -exec mv {} /temp \;'
chown -R "$current_user" "$package_temporary_dir"
if [ $? -ne 0 ]; then
	# Some platforms need this to chown the file properly. (Platforms with native docker, not just the client)
    sudo chown -R "$current_user" "$package_temporary_dir"
fi
mv "$package_temporary_dir"/* "$output_dir"
