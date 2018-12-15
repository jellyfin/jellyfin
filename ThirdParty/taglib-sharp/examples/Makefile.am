READ_FROM_URI = ReadFromUri
SET_PICTURES = SetPictures
LIST_TYPES = ListSupportedMimeTypes
BATCH_SET = BatchSet
PARSE_PHOTO = ParsePhoto
STRIP_IMAGE = StripImageData
GENERATE_TEST_FIXTURE = GenerateTestFixture

REFERENCES = -r:taglib-sharp.dll
EXAMPLES_BUILD = taglib-sharp.dll $(SET_PICTURES).exe $(LIST_TYPES).exe $(BATCH_SET).exe $(PARSE_PHOTO).exe $(STRIP_IMAGE).exe

if HAVE_GNOME_SHARP
EXAMPLES_BUILD += $(READ_FROM_URI).exe
if HAVE_EXIV2
EXAMPLES_BUILD += $(GENERATE_TEST_FIXTURE).exe
endif
endif

all: $(EXAMPLES_BUILD)

taglib-sharp.dll:
	if [ ! -e $@ ]; then ln -s $(top_builddir)/src/taglib-sharp.dll $@; fi

if HAVE_GNOME_SHARP
$(READ_FROM_URI).exe: $(READ_FROM_URI).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $(GNOME_SHARP_LIBS) $<

if HAVE_EXIV2
$(GENERATE_TEST_FIXTURE).exe: $(GENERATE_TEST_FIXTURE).cs listData extractKey
	$(MCS) -out:$@ -debug $(REFERENCES) $(GNOME_SHARP_LIBS) $<

listData: listData.cpp
	g++ -o $@ $(EXIV2_LIBS) $<

extractKey: extractKey.cpp
	g++ -o $@ $(EXIV2_LIBS) $<
endif
endif

$(SET_PICTURES).exe: $(SET_PICTURES).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $<

$(LIST_TYPES).exe: $(LIST_TYPES).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $<

$(BATCH_SET).exe: $(BATCH_SET).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $<

$(PARSE_PHOTO).exe: $(PARSE_PHOTO).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $<

$(STRIP_IMAGE).exe: $(STRIP_IMAGE).cs
	$(MCS) -out:$@ -debug $(REFERENCES) $<

EXTRA_DIST = $(READ_FROM_URI).cs $(SET_PICTURES).cs $(LIST_TYPES).cs $(BATCH_SET).cs $(PARSE_PHOTO).cs $(STRIP_IMAGE).cs $(GENERATE_TEST_FIXTURE).cs \
	$(srcdir)/covers/sample*

DISTCLEANFILES = *.pidb
CLEANFILES = *.dll *.exe *.mdb
MAINTAINERCLEANFILES = Makefile.in

