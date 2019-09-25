#!/usr/bin/env bash

# shellcheck disable=SC1091
WORKDIR="$( pwd )"
VERSION="$( sed -ne '/^Version:/s/.*  *//p' "${WORKDIR}"/pkg-src/jellyfin.spec )"

package_temporary_dir="${WORKDIR}/pkg-dist-tmp"
pkg_src_dir="${WORKDIR}/pkg-src"

GNU_TAR=1
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
-czf "$pkg_src_dir/jellyfin-${VERSION}.tar.gz" \
-C "../.." ./ || GNU_TAR=0

if [ $GNU_TAR -eq 0 ]; then
    echo "The installed tar binary did not support --transform. Using workaround."
    mkdir -p "${package_temporary_dir}/jellyfin"{,-"${VERSION}"}
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
