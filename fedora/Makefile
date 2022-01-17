DIR     := $(dir $(lastword $(MAKEFILE_LIST)))
INSTGIT := $(shell if [ "$$(id -u)" = "0" ]; then dnf -y install git; fi)
NAME    := jellyfin-server
VERSION := $(shell sed -ne '/^Version:/s/.*  *//p' $(DIR)/jellyfin.spec)
RELEASE := $(shell sed -ne '/^Release:/s/.*  *\(.*\)%{.*}.*/\1/p' $(DIR)/jellyfin.spec)
SRPM    := jellyfin-$(subst -,~,$(VERSION))-$(RELEASE)$(shell rpm --eval %dist).src.rpm
TARBALL :=$(NAME)-$(subst -,~,$(VERSION)).tar.gz

epel-7-x86_64_repos := https://packages.microsoft.com/rhel/7/prod/
epel-8-x86_64_repos := https://download.copr.fedorainfracloud.org/results/@dotnet-sig/dotnet-preview/$(TARGET)/
fedora_repos        := https://download.copr.fedorainfracloud.org/results/@dotnet-sig/dotnet-preview/$(TARGET)/
fedora-34-x86_64_repos := $(fedora_repos)
fedora-35-x86_64_repos := $(fedora_repos)
fedora-34-x86_64_repos := $(fedora_repos)

outdir  ?= $(PWD)/$(DIR)/
TARGET  ?= fedora-35-x86_64

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
