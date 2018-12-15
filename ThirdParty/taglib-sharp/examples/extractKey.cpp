#include <exiv2/image.hpp>
#include <exiv2/exif.hpp>
#include <iostream>
#include <iomanip>
#include <cassert>

int main(int argc, char* const argv[])

try {
    if (argc != 3) {
        std::cout << "Usage: " << argv[0] << " file key\n";
        return 1;
    }

    Exiv2::Image::AutoPtr image = Exiv2::ImageFactory::open(argv[1]);
    assert(image.get() != 0);
    image->readMetadata();

	try {
		Exiv2::ExifData &exifData = image->exifData();
		const Exiv2::Value &value = exifData[argv[2]].value();
		std::cout << value;
	} catch (Exiv2::AnyError &e) {}

	try {
		Exiv2::IptcData &iptcData = image->iptcData();
		const Exiv2::Value &value = iptcData[argv[2]].value();
		std::cout << value;
	} catch (Exiv2::AnyError &e) {}

	try {
		Exiv2::XmpData &xmpData = image->xmpData();
		const Exiv2::Value &value = xmpData[argv[2]].value();
		std::cout << value;
	} catch (Exiv2::AnyError &e) {}

    return 0;
} catch (Exiv2::AnyError& e) {
    std::cout << "Caught Exiv2 exception '" << e << "'\n";
    return -1;
}
