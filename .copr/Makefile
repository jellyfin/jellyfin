VERSION := $(shell sed -ne '/^Version:/s/.*  *//p'                      \
                   deployment/fedora-package-x64/pkg-src/jellyfin.spec)

deployment/fedora-package-x64/pkg-src/jellyfin-web-$(VERSION).tar.gz:
	curl -f -L -o deployment/fedora-package-x64/pkg-src/jellyfin-web-$(VERSION).tar.gz \
         https://github.com/jellyfin/jellyfin-web/archive/v$(VERSION).tar.gz \
	|| curl -f -L -o deployment/fedora-package-x64/pkg-src/jellyfin-web-$(VERSION).tar.gz \
         https://github.com/jellyfin/jellyfin-web/archive/master.tar.gz \

srpm: deployment/fedora-package-x64/pkg-src/jellyfin-web-$(VERSION).tar.gz
	cd deployment/fedora-package-x64;                                             \
    SOURCE_DIR=../..                                                              \
    WORKDIR="$${PWD}";                                                            \
    package_temporary_dir="$${WORKDIR}/pkg-dist-tmp";                             \
    pkg_src_dir="$${WORKDIR}/pkg-src";                                            \
    GNU_TAR=1;                                                                    \
    tar                                                                           \
    --transform "s,^\.,jellyfin-$(VERSION),"                                      \
    --exclude='.git*'                                                             \
    --exclude='**/.git'                                                           \
    --exclude='**/.hg'                                                            \
    --exclude='**/.vs'                                                            \
    --exclude='**/.vscode'                                                        \
    --exclude='deployment'                                                        \
    --exclude='**/bin'                                                            \
    --exclude='**/obj'                                                            \
    --exclude='**/.nuget'                                                         \
    --exclude='*.deb'                                                             \
    --exclude='*.rpm'                                                             \
    -czf "pkg-src/jellyfin-$(VERSION).tar.gz"                                     \
    -C $${SOURCE_DIR} ./ || GNU_TAR=0;                                            \
    if [ $${GNU_TAR} -eq 0 ]; then                                                \
        package_temporary_dir="$$(mktemp -d)";                                    \
        mkdir -p "$${package_temporary_dir}/jellyfin";                            \
        tar                                                                       \
        --exclude='.git*'                                                         \
        --exclude='**/.git'                                                       \
        --exclude='**/.hg'                                                        \
        --exclude='**/.vs'                                                        \
        --exclude='**/.vscode'                                                    \
        --exclude='deployment'                                                    \
        --exclude='**/bin'                                                        \
        --exclude='**/obj'                                                        \
        --exclude='**/.nuget'                                                     \
        --exclude='*.deb'                                                         \
        --exclude='*.rpm'                                                         \
        -czf "$${package_temporary_dir}/jellyfin/jellyfin-$(VERSION).tar.gz"      \
        -C $${SOURCE_DIR} ./;                                                     \
        mkdir -p "$${package_temporary_dir}/jellyfin-$(VERSION)";                 \
        tar -xzf "$${package_temporary_dir}/jellyfin/jellyfin-$(VERSION).tar.gz"  \
            -C "$${package_temporary_dir}/jellyfin-$(VERSION);                    \
        rm -f "$${package_temporary_dir}/jellyfin/jellyfin-$(VERSION).tar.gz";    \
        tar -czf "$${SOURCE_DIR}/SOURCES/pkg-src/jellyfin-$(VERSION).tar.gz"      \
            -C "$${package_temporary_dir}" "jellyfin-$(VERSION);                  \
        rm -rf $${package_temporary_dir};                                         \
	fi;                                                                           \
	rpmbuild -bs pkg-src/jellyfin.spec                                            \
	         --define "_sourcedir $$PWD/pkg-src/"                                 \
	         --define "_srcrpmdir $(outdir)"
