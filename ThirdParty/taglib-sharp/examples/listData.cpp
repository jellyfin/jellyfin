#include <exiv2/image.hpp>
#include <exiv2/exif.hpp>
#include <iostream>
#include <iomanip>
#include <cassert>

int main(int argc, char* const argv[])

try {
    if (argc != 3) {
        std::cout << "Usage: " << argv[0] << " mode file\n";
        return 1;
    }

    Exiv2::Image::AutoPtr image = Exiv2::ImageFactory::open(argv[2]);
    assert(image.get() != 0);
    image->readMetadata();

	if (argv[1][0] == 'e') {
		Exiv2::ExifData &exifData = image->exifData();
		if (!exifData.empty()) {
			Exiv2::ExifData::const_iterator exifEnd = exifData.end();
			for (Exiv2::ExifData::const_iterator i = exifData.begin(); i != exifEnd; ++i) {
				std::cout << i->tagName() << "\t"
					  << "0x" << std::setw(4) << std::setfill('0') << std::right
					  << std::hex << i->tag() << std::dec << "\t"
					  << i->groupName() << "\t"
					  << i->typeName() << "\t"
					  << i->count() << "\t"
					  << "\n";
			}
		}
	}

	if (argv[1][0] == 'i') {
		Exiv2::IptcData &iptcData = image->iptcData();
		if (!iptcData.empty()) {
			Exiv2::IptcData::const_iterator iptcEnd = iptcData.end();
			for (Exiv2::IptcData::const_iterator i = iptcData.begin(); i != iptcEnd; ++i) {
				std::cout << i->key() << "\n";
			}
		}
	}

	if (argv[1][0] == 'x') {
		Exiv2::XmpData &xmpData = image->xmpData();
		if (!xmpData.empty()) {
			Exiv2::XmpData::const_iterator xmpEnd = xmpData.end();
			for (Exiv2::XmpData::const_iterator i = xmpData.begin(); i != xmpEnd; ++i) {
				std::cout << i->key() << "\t"
					  << i->typeName() << "\t"
					  << i->count() << "\t"
					  << "\n";
			}
		}
	}

    return 0;
} catch (Exiv2::AnyError& e) {
    std::cout << "Caught Exiv2 exception '" << e << "'\n";
    return -1;
}
