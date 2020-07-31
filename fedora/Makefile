VERSION := $(shell sed -ne '/^Version:/s/.*  *//p' fedora/jellyfin.spec)

srpm:
	cd fedora/;                                                                   \
	SOURCE_DIR=..                                                                 \
	WORKDIR="$${PWD}";                                                            \
	tar                                                                           \
	--transform "s,^\.,jellyfin-server-$(VERSION),"                               \
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
	--exclude='jellyfin-server-$(VERSION).tar.gz'                                 \
	-czf "jellyfin-server-$(VERSION).tar.gz"                                      \
	-C $${SOURCE_DIR} ./
	cd fedora/;                                                                   \
	rpmbuild -bs jellyfin.spec                                                    \
	         --define "_sourcedir $$PWD/"                                         \
	         --define "_srcrpmdir $(outdir)"
