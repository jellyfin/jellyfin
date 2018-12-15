SUBDIRS = \
   src    \
   examples \
   docs

if !IN_DIST
SUBDIRS += tests
endif

DIST_SUBDIRS = \
   src    \
   examples \
   docs

pkgconfig_in_files = taglib-sharp.pc.in
pkgconfigdir = $(datadir)/pkgconfig
pkgconfig_DATA = $(pkgconfig_in_files:.pc.in=.pc)

EXTRA_DIST =             \
   $(pkgconfig_in_files) \
   taglib-sharp.snk \
   taglib-sharp.sln

DISTCLEANFILES = taglib-sharp.pc

MAINTAINERCLEANFILES = \
	compile \
	INSTALL \
	config.h.in \
	aclocal.m4 \
	ltmain.sh \
	Makefile.in \
	depcomp \
	missing \
	install-sh \
	configure \
	config.sub \
	config.guess \
	mkinstalldirs

if !IN_DIST
test:
	cd tests; make test; cd ..;
endif

distwin: all
	mkdir -p $(srcdir)/$(WINDIR)
	cp $(srcdir)/NEWS $(srcdir)/$(WINDIR)/NEWS.txt
	cp $(srcdir)/ChangeLog $(srcdir)/$(WINDIR)/ChangeLog.txt
	cp $(srcdir)/COPYING $(srcdir)/$(WINDIR)/COPYING.txt
	cp $(srcdir)/README.md $(srcdir)/$(WINDIR)/README.txt
	cp $(srcdir)/AUTHORS $(srcdir)/$(WINDIR)/AUTHORS.txt
	if [ -e $(srcdir)/windows-files ];					\
	  then cp -r $(srcdir)/windows-files/* $(srcdir)/$(WINDIR);		\
	fi
	cd $(srcdir)/src && make distwin
	zip -9 -r $(srcdir)/$(WINDIR).zip $(srcdir)/$(WINDIR)
	rm -rf $(srcdir)/$(WINDIR)

UPLOAD_HOST = "$$BANSHEE_USER@banshee.fm"
UPLOAD_PATH = "web/download.banshee.fm/taglib-sharp"
push-release: distcheck distwin
	[[ -z "$$BANSHEE_USER" ]] && { echo "Set BANSHEE_USER"; exit 1; } || true
	mkdir -p release
	mv $(WINDIR).zip release
	mv $(PACKAGE)-$(VERSION).tar.* release
	cp ChangeLog release/$(PACKAGE)-$(VERSION).changes
	cp NEWS release/$(PACKAGE)-$(VERSION).news
	pushd release; sha256sum * > $(PACKAGE)-$(VERSION).sha256sum; popd
	scp -r release $(UPLOAD_HOST):$(UPLOAD_PATH)/$(VERSION)
	rm -rf release
	ssh $(UPLOAD_HOST) rm -f $(UPLOAD_PATH)/LATEST-IS\*
	ssh $(UPLOAD_HOST) touch $(UPLOAD_PATH)/LATEST-IS-$(VERSION)
	git tag -a -m "Tag for taglib-sharp release ${VERSION}" taglib-sharp-${VERSION} || bail "Could not create tag"
	git push origin taglib-sharp-${VERSION} || bail "Failed to push tag to remote"
