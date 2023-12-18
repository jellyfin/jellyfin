DIR     := $(dir $(lastword $(MAKEFILE_LIST)))
INSTGIT := $(shell if [ "$$(id -u)" = "0" ]; then dnf -y install git; fi)
NAME    := jellyfin-server
VERSION := $(shell sed -ne '/^Version:/s/.*  *//p' $(DIR)/jellyfin.spec)
RELEASE := $(shell sed -ne '/^Release:/s/.*  *\(.*\)%{.*}.*/\1/p' $(DIR)/jellyfin.spec)
SRPM    := jellyfin-$(subst -,~,$(VERSION))-$(RELEASE)$(shell rpm --eval %dist).src.rpm
TARBALL :=$(NAME)-$(subst -,~,$(VERSION)).tar.gz

epel-7-x86_64_repos := https://packages.microsoft.com/rhel/7/prod/

fed_ver := $(shell rpm -E %fedora)
# fallback when not running on Fedora
fed_ver ?= 36
TARGET  ?= fedora-$(fed_ver)-x86_64

outdir  ?= $(PWD)/$(DIR)/

srpm: $(DIR)/$(SRPM)
tarball: $(DIR)/$(TARBALL)

$(DIR)/$(TARBALL):
	cd $(DIR)/;                                          \
	SOURCE_DIR=..                                        \
	WORKDIR="$${PWD}";                                   \
	version=$(VERSION);                                  \
	tar                                                  \
	--transform "s,^\.,$(NAME)-$(subst -,~,$(VERSION))," \
	--exclude='.git*'                                    \
	--exclude='**/.git'                                  \
	--exclude='**/.hg'                                   \
	--exclude='**/.vs'                                   \
	--exclude='**/.vscode'                               \
	--exclude=deployment                                 \
	--exclude='**/bin'                                   \
	--exclude='**/obj'                                   \
	--exclude='**/.nuget'                                \
	--exclude='*.deb'                                    \
	--exclude='*.rpm'                                    \
	--exclude=$(notdir $@)                               \
	-czf $(notdir $@)                                    \
	-C $${SOURCE_DIR} ./

$(DIR)/$(SRPM): $(DIR)/$(TARBALL) $(DIR)/jellyfin.spec
	cd $(DIR)/;                              \
	rpmbuild -bs jellyfin.spec               \
	         --define "_sourcedir $$PWD/"    \
	         --define "_srcrpmdir $(outdir)"

rpms: $(DIR)/$(SRPM)
	mock $(addprefix --addrepo=, $($(TARGET)_repos)) \
	     --enable-network                            \
	     -r $(TARGET) $<
