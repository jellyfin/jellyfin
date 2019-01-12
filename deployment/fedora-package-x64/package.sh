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
pkg_src_dir="`pwd`/pkg-src"
current_user="`whoami`"
image_name="jellyfin-rpmbuild"

cleanup() {
    set +o errexit
    docker image rm $image_name --force
    rm -rf "$package_temporary_dir"
    rm -rf "$pkg_src_dir/jellyfin-${VERSION}.tar.gz"
}
trap cleanup EXIT INT
GNU_TAR=1
mkdir -p "$package_temporary_dir"
echo "Bundling all sources for RPM build."
tar \
--transform "s,^\.,jellyfin-${VERSION}" \
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
-Jcvf \
"$package_temporary_dir/jellyfin-${VERSION}.tar.xz" \
-C "../.." \
./ || true && GNU_TAR=0

if [ $GNU_TAR -eq 0 ]; then
    echo "The installed tar binary did not support --transform. Using workaround."
    mkdir -p "$package_temporary_dir/jellyfin-${VERSION}"
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
    "$package_temporary_dir/jellyfin-${VERSION}/jellyfin.tar.gz" \
    -C "../.." \
    ./
    echo "Extracting filtered package."
    tar -xzf "$package_temporary_dir/jellyfin-${VERSION}/jellyfin.tar.gz" -C "$package_temporary_dir/jellyfin-${VERSION}"
    echo "Removing filtered package."
    rm "$package_temporary_dir/jellyfin-${VERSION}/jellyfin.tar.gz"
    echo "Repackaging package into final tarball."
    tar -zcf "$pkg_src_dir/jellyfin-${VERSION}.tar.gz" -C "$package_temporary_dir" "jellyfin-${VERSION}"
fi

docker build ../.. -t "$image_name" -f ./Dockerfile
mkdir -p "$output_dir"
docker run --rm -v "$package_temporary_dir:/temp" "$image_name" sh -c 'find /build/rpmbuild -maxdepth 3 -type f -name "jellyfin*.rpm" -exec mv {} /temp \;'
chown -R "$current_user" "$package_temporary_dir"
if [ $? -ne 0 ]; then
	# Some platforms need this to chown the file properly. (Platforms with native docker, not just the client)
    sudo chown -R "$current_user" "$package_temporary_dir"
fi
mv "$package_temporary_dir"/*.rpm "$output_dir"
