AC_INIT([taglib-sharp], [2.1.0.0])
AM_INIT_AUTOMAKE([1.9 dist-bzip2 tar-ustar foreign])
AM_MAINTAINER_MODE

ASSEMBLY_NAME="$PACKAGE"
ASSEMBLY_VERSION="$VERSION"

POLICY_2_0_VERSIONS="2.0.0.0-$ASSEMBLY_VERSION"
AC_SUBST(POLICY_2_0_VERSIONS)

AC_SUBST(VERSION)
AC_SUBST(ASSEMBLY_VERSION)
AC_SUBST(ASSEMBLY_NAME)

AC_PROG_INSTALL
AC_PROG_GREP

MONODOC_REQUIRED=1.1.9

WINDIR="$ASSEMBLY_NAME-$VERSION-windows"
AC_SUBST(WINDIR)

DISTCHECK_CONFIGURE_FLAGS="--disable-docs"
AC_SUBST(DISTCHECK_CONFIGURE_FLAGS)

dnl Check for Mono
AC_PATH_PROG(MCS, mcs)
if test x$MCS = x; then
	AC_MSG_ERROR(You need mcs)
fi

AC_PATH_PROG(MONO, mono)
if test x$MONO = x; then
	AC_MSG_ERROR(You need mono)
fi

MCS_FLAGS="-codepage:utf8 -debug"
MONO_FLAGS=

if test $USE_MAINTAINER_MODE = yes; then
	MONO_FLAGS="$MONO_FLAGS --debug"
fi

AC_SUBST(MCS_FLAGS)
AC_SUBST(MONO_FLAGS)

AC_PATH_PROG(AL, al)
if test "x$AL" = "x"; then
	AC_MSG_ERROR([No al tool found. You need to install mono])
fi

dnl GAC
AC_PATH_PROG(GACUTIL, gacutil)
if test x$GACUTIL = x; then
	AC_MSG_ERROR(You need gacutil)
fi

GACUTIL_FLAGS='/package $(ASSEMBLY_NAME) /gacdir $(prefix)/lib /root $(DESTDIR)$(prefix)/lib'
AC_SUBST(GACUTIL_FLAGS)
GACUTIL_POLICY_FLAGS='/gacdir $(prefix)/lib /root $(DESTDIR)$(prefix)/lib'
AC_SUBST(GACUTIL_POLICY_FLAGS)

AC_ARG_ENABLE(docs, AC_HELP_STRING([--disable-docs], [Do not build documentation]), with_docs=no, with_docs=yes)

PKG_PROG_PKG_CONFIG

dnl Monodoc
if test "x$with_docs" = "xyes"; then
	AC_PATH_PROG(MONODOCER, monodocer, no)
	if test "x$MONODOCER" = "xno"; then
		AC_MSG_ERROR([You need to install monodoc, or pass --disable-docs to configure to skip documentation installation])
	fi

	AC_PATH_PROG(MDASSEMBLER, mdassembler, no)
	if test "x$MDASSEMBLER" = "xno"; then
		AC_MSG_ERROR([You need to install mdassembler, or pass --disable-docs to configure to skip documentation installation])
	fi

	DOCDIR=`$PKG_CONFIG monodoc --variable=sourcesdir`
	AC_SUBST(DOCDIR)
	AM_CONDITIONAL(BUILD_DOCS, true)
else
	AC_MSG_NOTICE([not building Banshee API documentation])
	AM_CONDITIONAL(BUILD_DOCS, false)
fi

PKG_CHECK_MODULES(GNOME_SHARP, gnome-sharp-2.0, have_gnome_sharp=yes, have_gnome_sharp=no)
if test "x$have_gnome_sharp" = "xyes"; then
	AC_SUBST(GNOME_SHARP_LIBS)
	AM_CONDITIONAL(HAVE_GNOME_SHARP, true)
else
	AM_CONDITIONAL(HAVE_GNOME_SHARP, false)
fi

PKG_CHECK_MODULES(EXIV2, exiv2, have_exiv2=yes, have_exiv2=no)
if test "x$have_exiv2" = "xyes"; then
	AC_SUBST(EXIV2_LIBS)
	AM_CONDITIONAL(HAVE_EXIV2, true)
else
	AM_CONDITIONAL(HAVE_EXIV2, false)
fi

PKG_CHECK_MODULES(MONO_NUNIT, mono-nunit >= 1.0, do_tests="yes", do_tests="no") AC_SUBST(MONO_NUNIT_LIBS)
AM_CONDITIONAL(ENABLE_TESTS, test "x$do_tests" = "xyes")
if test "x$do_tests" = "xno"; then
	AC_MSG_WARN([Could not find mono-nunit: tests will not be available.])
else
	AC_PATH_PROG(NUNIT_CMD, nunit-console2, nunit-console)
	AC_SUBST(NUNIT_CMD)
fi

AC_ARG_ENABLE(raw-tests, AC_HELP_STRING([--enable-raw-tests], [Enable RAW image files NUnit tests]),
		enable_raw_tests=$enableval, enable_raw_tests="no")
if test "x$enable_raw_tests" = "xno"; then
	AC_MSG_WARN([RAW image file tests disabled.])
	AM_CONDITIONAL(ENABLE_RAW_TESTS, false)
else
	AM_CONDITIONAL(ENABLE_RAW_TESTS, true)
fi

AC_CONFIG_FILES([
Makefile
taglib-sharp.pc
src/Makefile
src/policy.2.0.taglib-sharp.config
examples/Makefile
docs/Makefile
docs/Package.en.xml
])

# Test suite is not shipped, only builds from git.
if test -d tests; then
	AC_CONFIG_FILES([tests/Makefile])
	AM_CONDITIONAL(IN_DIST, false)
else
	AM_CONDITIONAL(IN_DIST, true)
fi
AC_OUTPUT

echo "TagLib# ($ASSEMBLY_NAME-$ASSEMBLY_VERSION) is ready to be compiled."
