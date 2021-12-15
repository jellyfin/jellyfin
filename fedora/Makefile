VERSION := $(shell sed -ne '/^Version:/s/.*  *//p' fedora/jellyfin.spec)
outdir  ?= fedora/
TARGET  ?= fedora-35-x86_64

srpm:
	pushd fedora/;                                              \
	if [ "$$(id -u)" = "0" ]; then                              \
	    dnf -y install git;                                     \
	fi;                                                         \
	version=$$(git describe --tags | sed -e 's/^v//'            \
	                                     -e 's/-[0-9]*-g.*$$//' \
	                                     -e 's/-/~/');          \
	SOURCE_DIR=..                                               \
	WORKDIR="$${PWD}";                                          \
	tar                                                         \
	--transform "s,^\.,jellyfin-server-$$version,"              \
	--exclude='.git*'                                           \
	--exclude='**/.git'                                         \
	--exclude='**/.hg'                                          \
	--exclude='**/.vs'                                          \
	--exclude='**/.vscode'                                      \
	--exclude=deployment                                        \
	--exclude='**/bin'                                          \
	--exclude='**/obj'                                          \
	--exclude='**/.nuget'                                       \
	--exclude='*.deb'                                           \
	--exclude='*.rpm'                                           \
	--exclude=jellyfin-server-$$version.tar.gz                  \
	-czf "jellyfin-server-$$version.tar.gz"                     \
	-C $${SOURCE_DIR} ./;                                       \
	popd;                                                       \
	./bump_version $$version
	cd fedora/;                              \
	rpmbuild -bs jellyfin.spec               \
	         --define "_sourcedir $$PWD/"    \
	         --define "_srcrpmdir $(outdir)"

rpms: fedora/jellyfin-$(shell git describe --tags | sed -e 's/^v//' -e 's/-[0-9]*-g.*$$//' -e 's/-/~/')-1$(shell rpm --eval %dist).src.rpm
	mock --addrepo=https://download.copr.fedorainfracloud.org/results/@dotnet-sig/dotnet-preview/$(TARGET)/ \
	     --enable-network                                                                                   \
	     -r $(TARGET) $<
