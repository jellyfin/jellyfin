MCS_FLAGS = -debug
NUNIT_FLAGS = @MONO_NUNIT_LIBS@

ASSEMBLY_NAME = tests
ASSEMBLY = $(ASSEMBLY_NAME).dll
ASSEMBLY_CSFILES =  \
	fixtures/Helpers.cs \
	fixtures/TagLib.Tests.Collections/ByteVectorCollectionTest.cs \
	fixtures/TagLib.Tests.Collections/ByteVectorTest.cs \
	fixtures/TagLib.Tests.Collections/FileTest.cs \
	fixtures/TagLib.Tests.Collections/ImageBlockFileTest.cs \
	fixtures/TagLib.Tests.Collections/StringCollectionTest.cs \
	fixtures/TagLib.Tests.FileFormats/AacFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/AiffFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/AsfFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/AviFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/DsfFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/FlacFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/Id3BothFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/Id3V1FormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/Id3V24FormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/Id3V2FormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/IFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/JpegFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/M4aFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/M4vFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/MpcFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/OggFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/OpusFormatTest.cs \
	fixtures/TagLib.Tests.FileFormats/StandardTests.cs \
	fixtures/TagLib.Tests.Images/AddImageMetadataTests.cs \
	fixtures/TagLib.Tests.Images/BadCommentMarkerTest.cs \
	fixtures/TagLib.Tests.Images/BGO493530Test.cs \
	fixtures/TagLib.Tests.Images/BGO625367Test.cs \
	fixtures/TagLib.Tests.Images/BGO631333Test.cs \
	fixtures/TagLib.Tests.Images/CopyFromTest.cs \
	fixtures/TagLib.Tests.Images/EmptyCommentTest.cs \
	fixtures/TagLib.Tests.Images/GifExiftoolLongCommentTest.cs \
	fixtures/TagLib.Tests.Images/GifExiftoolTangled1Test.cs \
	fixtures/TagLib.Tests.Images/GifExiftoolTangled2Test.cs \
	fixtures/TagLib.Tests.Images/GifExiftoolTangled3Test.cs \
	fixtures/TagLib.Tests.Images/GifExiftoolTest.cs \
	fixtures/TagLib.Tests.Images/GifGimpTest.cs \
	fixtures/TagLib.Tests.Images/InvalidIFDEntryTypeTest.cs \
	fixtures/TagLib.Tests.Images/InvalidIFDOffsetTest.cs \
	fixtures/TagLib.Tests.Images/IptcTest.cs \
	fixtures/TagLib.Tests.Images/JpegBGO624097Test.cs \
	fixtures/TagLib.Tests.Images/JpegCanonBibble5Test.cs \
	fixtures/TagLib.Tests.Images/JpegCanonZoombrowserTest.cs \
	fixtures/TagLib.Tests.Images/JpegNikon1Bibble5Test.cs \
	fixtures/TagLib.Tests.Images/JpegNikon1Test.cs \
	fixtures/TagLib.Tests.Images/JpegNikon2Bibble5Test.cs \
	fixtures/TagLib.Tests.Images/JpegNikon2Test.cs \
	fixtures/TagLib.Tests.Images/JpegNikon3Test.cs \
	fixtures/TagLib.Tests.Images/JpegNikon4Test.cs \
	fixtures/TagLib.Tests.Images/JpegNoMetadataTest.cs \
	fixtures/TagLib.Tests.Images/JpegOlympus1Test.cs \
	fixtures/TagLib.Tests.Images/JpegOlympus2Test.cs \
	fixtures/TagLib.Tests.Images/JpegOlympus3Test.cs \
	fixtures/TagLib.Tests.Images/JpegOlympusCorruptMNTest.cs \
	fixtures/TagLib.Tests.Images/JpegPanasonicTest.cs \
	fixtures/TagLib.Tests.Images/JpegPropertyTest.cs \
	fixtures/TagLib.Tests.Images/JpegSegmentSizeTest.cs \
	fixtures/TagLib.Tests.Images/JpegSony1Test.cs \
	fixtures/TagLib.Tests.Images/JpegSony2Test.cs \
	fixtures/TagLib.Tests.Images/JpegTangledTest.cs \
	fixtures/TagLib.Tests.Images/NullOrientationTest.cs \
	fixtures/TagLib.Tests.Images/PngGimpPngcrushTest.cs \
	fixtures/TagLib.Tests.Images/PngGimpTest.cs \
	fixtures/TagLib.Tests.Images/RationalTest.cs \
	fixtures/TagLib.Tests.Images/RecursiveIFDTest.cs \
	fixtures/TagLib.Tests.Images/TiffCanonBibble516BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffCanonBibble58BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffCanonDpp16BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffCanonDpp8BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffGimp1Test.cs \
	fixtures/TagLib.Tests.Images/TiffGimp2Test.cs \
	fixtures/TagLib.Tests.Images/TiffNikon1Bibble516BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon1Bibble58BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon1ViewNX16BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon1ViewNX8BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon2Bibble516BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon2Bibble58BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon2ViewNX16BitTest.cs \
	fixtures/TagLib.Tests.Images/TiffNikon2ViewNX8BitTest.cs \
	fixtures/TagLib.Tests.Images/Utils.cs \
	fixtures/TagLib.Tests.Images/Validators/CommentModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/ImageTest.cs \
	fixtures/TagLib.Tests.Images/Validators/IMetadataInvariantValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/IMetadataModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/KeywordsModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/NoModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/PropertyModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/RemoveMetadataValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/TagCommentModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/TagKeywordsModificationValidator.cs \
	fixtures/TagLib.Tests.Images/Validators/TagPropertyModificationValidator.cs \
	fixtures/TagLib.Tests.Images/XapTest.cs \
	fixtures/TagLib.Tests.Images/XmpCrashTest.cs \
	fixtures/TagLib.Tests.Images/XmpSpecTest.cs \
	fixtures/TagLib.Tests.Performance/PerformanceTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/ApeTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/AsfTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/DivXTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/Id3V1Test.cs \
	fixtures/TagLib.Tests.TaggingFormats/Id3V2Test.cs \
	fixtures/TagLib.Tests.TaggingFormats/InfoTagTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/MovieIdTagTest.cs \
	fixtures/TagLib.Tests.TaggingFormats/Mpeg4Test.cs \
	fixtures/TagLib.Tests.TaggingFormats/XiphTest.cs

RAW_CSFILES = \
	fixtures/TagLib.Tests.Images/ArwSonyA700Test.cs \
	fixtures/TagLib.Tests.Images/BGO633972Test.cs \
	fixtures/TagLib.Tests.Images/Cr2Canon400d1Test.cs \
	fixtures/TagLib.Tests.Images/DngLeicaM8Test.cs \
	fixtures/TagLib.Tests.Images/ImageTagTests.cs \
	fixtures/TagLib.Tests.Images/NefNikonD90Test.cs \
	fixtures/TagLib.Tests.Images/PefPentaxKd10Test.cs \
	fixtures/TagLib.Tests.Images/RawLeicaDigilux2Test.cs \
	fixtures/TagLib.Tests.Images/Rw2PanasonicG1Test.cs


$(ASSEMBLY): $(ASSEMBLY_CSFILES)
	$(MCS) $(MCS_FLAGS) $(NUNIT_FLAGS) $(GNOME_SHARP_LIBS) -out:$@ -target:library -r:$(top_builddir)/src/taglib-sharp.dll $(ASSEMBLY_CSFILES)

TAGLIB_ASM = ../src/taglib-sharp.dll

if ENABLE_RAW_TESTS
update: $(RAW_CSFILES)
	@echo "Updating RAW repository (this may take a very long time)"
	pushd $(top_srcdir); git submodule update --init; popd

ASSEMBLY_CSFILES += $(RAW_CSFILES)
else
update:
endif

build: update $(ASSEMBLY) $(TAGLIB_ASM)

if ENABLE_TESTS
test: build
	MONO_PATH="../src" COMPARE_LARGE_FILES="$(COMPARE_LARGE_FILES)" $(NUNIT_CMD) -noshadow $(ASSEMBLY) $(ARGS)
endif

CLEANFILES = $(ASSEMBLY) TestResult.xml *.mdb *.dll *.exe
DISTCLEANFILES = *.pidb
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = \
	$(ASSEMBLY_CSFILES) \
	$(srcdir)/samples/sample* \
	$(srcdir)/samples/vector.bin \
	$(srcdir)/samples/LICENSE \
	$(srcdir)/samples/corrupt/a.* \
	$(srcdir)/samples/corrupt/null_title_v2.mp3 \
	$(srcdir)/samples/unsupported/sample*

