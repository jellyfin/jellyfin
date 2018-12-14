#!/usr/bin/env sh

# Build a Jellyfin .deb file with Docker on Linux
# Places the output .deb file in the parent directory

set -o xtrace
set -o errexit
set -o nounset

package_temporary_dir="`mktemp -d`"
current_user="`whoami`"
image_name="jellyfin-debuild"

cleanup() {
    docker image rm $image_name --force
    test -d ${package_temporary_dir} && rm -r ${package_temporary_dir}
}
trap cleanup EXIT

docker build . -t $image_name -f ./Dockerfile.debian_package
docker run --rm -v $package_temporary_dir:/temp $image_name cp -r /dist /temp/
sudo chown -R $current_user $package_temporary_dir
mv $package_temporary_dir/dist/*.deb ../
