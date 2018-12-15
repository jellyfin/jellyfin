POLICIES = policy.2.0.$(ASSEMBLY_NAME).dll
ASSEMBLY = $(ASSEMBLY_NAME).dll
TARGET = $(ASSEMBLY)

if BUILD_DOCS
DOCFILE = $(ASSEMBLY).xml
DOCFLAGS = /doc:$(DOCFILE) /warn:4
else
DOCFILE =
DOCFLAGS =
endif

CSC = $(MCS) $(MCS_FLAGS) $(CSFLAGS)

include $(srcdir)/TagLib/TagLib.sources

taglib-sharp.snk: $(top_srcdir)/taglib-sharp.snk
	cp $(top_srcdir)/taglib-sharp.snk .

taglib_sources_in = AssemblyInfo.cs.in
taglib_generated_sources = $(taglib_sources_in:.in=)
taglib_policy_names = $(POLICIES:.dll=)
taglib_policy_configs = $(POLICIES:.dll=.config)
taglib_policy_configs_in = $(POLICIES:.dll=.config.in)

$(ASSEMBLY): $(TAGLIB_CSFILES) $(taglib_generated_sources) taglib-sharp.snk
	$(CSC) /target:library $(LIBFLAGS) $(DOCFLAGS) /define:SIGN /out:$@ $(TAGLIB_CSFILES) $(taglib_generated_sources)

policy.%.$(ASSEMBLY_NAME).dll: policy.%.$(ASSEMBLY_NAME).config
	$(AL) /link:$< /out:$@ /keyfile:taglib-sharp.snk

all: $(ASSEMBLY) $(POLICIES)

EXTRA_DIST = $(TAGLIB_CSFILES) $(taglib_sources_in) $(taglib_policy_configs_in) taglib-sharp.csproj
DISTCLEANFILES = *.pidb
CLEANFILES = $(ASSEMBLY) $(ASSEMBLY).mdb $(POLICIES) taglib-sharp.snk $(DOCFILE)
MAINTAINERCLEANFILES = Makefile.in

install-data-local:
	@if test -n '$(TARGET)'; then						\
	  echo "$(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS)";			\
	  $(GACUTIL) /i $(ASSEMBLY) /f $(GACUTIL_FLAGS) || exit 1;		\
	  for POLICY in $(POLICIES); 						\
	    do echo "$(GACUTIL) /i $$POLICY /f $(GACUTIL_POLICY_FLAGS)";	\
	    $(GACUTIL) /i $$POLICY /f $(GACUTIL_POLICY_FLAGS) || exit 1;	\
	  done									\
	fi

uninstall-local:
	@if test -n '$(TARGET)'; then						\
	  echo "$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)";		\
	  $(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;		\
	  for POLICY in $(taglib_policy_names);					\
	    do echo "$(GACUTIL) /u $$POLICY /f $(GACUTIL_POLICY_FLAGS)";	\
	    $(GACUTIL) /u $$POLICY /f $(GACUTIL_POLICY_FLAGS) || exit 1;	\
	  done									\
	fi

distwin:
	mkdir -p $(srcdir)/../$(WINDIR)/Libraries
	cp $(srcdir)/*.dll $(srcdir)/*.config $(srcdir)/../$(WINDIR)/Libraries
	for FILE in $(TAGLIB_CSFILES) $(taglib_generated_sources);		\
	  do mkdir -p $(srcdir)/../$(WINDIR)/src/`dirname $$FILE`;		\
	  cp $$FILE $(srcdir)/../$(WINDIR)/src/`dirname $$FILE`;		\
	done
