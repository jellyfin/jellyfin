srpm:
	dnf -y install git
	git submodule update --init --recursive
	cd deployment/fedora-package-x64;                    \
	./create_tarball.sh;                                 \
	rpmbuild -bs pkg-src/jellyfin.spec                   \
	         --define "_sourcedir $$PWD/pkg-src/"        \
		 --define "_srcrpmdir $(outdir)"
