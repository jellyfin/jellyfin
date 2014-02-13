using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace MediaBrowser.Providers.Photos
{
    /// <summary>
    /// A class for reading Exif data from a JPEG file. The file will be open for reading for as long as the class exists.
    /// <seealso cref="http://gvsoft.homedns.org/exif/Exif-explanation.html"/>
    /// </summary>
    public class ExifReader : IDisposable
    {
        private readonly FileStream fileStream = null;
        private readonly BinaryReader reader = null;

        /// <summary>
        /// The catalogue of tag ids and their absolute offsets within the
        /// file
        /// </summary>
        private Dictionary<ushort, long> catalogue;

        /// <summary>
        /// Indicates whether to read data using big or little endian byte aligns
        /// </summary>
        private bool isLittleEndian;

        /// <summary>
        /// The position in the filestream at which the TIFF header starts
        /// </summary>
        private long tiffHeaderStart;

        public ExifReader(string fileName)
        {
            // JPEG encoding uses big endian (i.e. Motorola) byte aligns. The TIFF encoding
            // found later in the document will specify the byte aligns used for the
            // rest of the document.
            isLittleEndian = false;

            try
            {
                // Open the file in a stream
                fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                reader = new BinaryReader(fileStream);

                // Make sure the file's a JPEG.
                if (ReadUShort() != 0xFFD8)
                    throw new Exception("File is not a valid JPEG");

                // Scan to the start of the Exif content
                ReadToExifStart();

                // Create an index of all Exif tags found within the document
                CreateTagIndex();
            }
            catch (Exception)
            {
                // If instantiation fails, make sure there's no mess left behind
                Dispose();

                throw;
            }
        }

        #region TIFF methods

        /// <summary>
        /// Returns the length (in bytes) per component of the specified TIFF data type
        /// </summary>
        /// <returns></returns>
        private byte GetTIFFFieldLength(ushort tiffDataType)
        {
            switch (tiffDataType)
            {
                case 1:
                case 2:
                case 6:
                    return 1;
                case 3:
                case 8:
                    return 2;
                case 4:
                case 7:
                case 9:
                case 11:
                    return 4;
                case 5:
                case 10:
                case 12:
                    return 8;
                default:
                    throw new Exception(string.Format("Unknown TIFF datatype: {0}", tiffDataType));
            }
        }

        #endregion

        #region Methods for reading data directly from the filestream

        /// <summary>
        /// Gets a 2 byte unsigned integer from the file
        /// </summary>
        /// <returns></returns>
        private ushort ReadUShort()
        {
            return ToUShort(ReadBytes(2));
        }

        /// <summary>
        /// Gets a 4 byte unsigned integer from the file
        /// </summary>
        /// <returns></returns>
        private uint ReadUint()
        {
            return ToUint(ReadBytes(4));
        }

        private string ReadString(int chars)
        {
            return Encoding.ASCII.GetString(ReadBytes(chars));
        }

        private byte[] ReadBytes(int byteCount)
        {
            return reader.ReadBytes(byteCount);
        }

        /// <summary>
        /// Reads some bytes from the specified TIFF offset
        /// </summary>
        /// <param name="tiffOffset"></param>
        /// <param name="byteCount"></param>
        /// <returns></returns>
        private byte[] ReadBytes(ushort tiffOffset, int byteCount)
        {
            // Keep the current file offset
            long originalOffset = fileStream.Position;

            // Move to the TIFF offset and retrieve the data
            fileStream.Seek(tiffOffset + tiffHeaderStart, SeekOrigin.Begin);

            byte[] data = reader.ReadBytes(byteCount);

            // Restore the file offset
            fileStream.Position = originalOffset;

            return data;
        }

        #endregion

        #region Data conversion methods for interpreting datatypes from a byte array

        /// <summary>
        /// Converts 2 bytes to a ushort using the current byte aligns
        /// </summary>
        /// <returns></returns>
        private ushort ToUShort(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToUInt16(data, 0);
        }

        /// <summary>
        /// Converts 8 bytes to an unsigned rational using the current byte aligns.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /// <seealso cref="ToRational"/>
        private double ToURational(byte[] data)
        {
            var numeratorData = new byte[4];
            var denominatorData = new byte[4];

            Array.Copy(data, numeratorData, 4);
            Array.Copy(data, 4, denominatorData, 0, 4);

            uint numerator = ToUint(numeratorData);
            uint denominator = ToUint(denominatorData);

            return numerator / (double)denominator;
        }

        /// <summary>
        /// Converts 8 bytes to a signed rational using the current byte aligns.
        /// </summary>
        /// <remarks>
        /// A TIFF rational contains 2 4-byte integers, the first of which is
        /// the numerator, and the second of which is the denominator.
        /// </remarks>
        /// <param name="data"></param>
        /// <returns></returns>
        private double ToRational(byte[] data)
        {
            var numeratorData = new byte[4];
            var denominatorData = new byte[4];

            Array.Copy(data, numeratorData, 4);
            Array.Copy(data, 4, denominatorData, 0, 4);

            int numerator = ToInt(numeratorData);
            int denominator = ToInt(denominatorData);

            return numerator / (double)denominator;
        }

        /// <summary>
        /// Converts 4 bytes to a uint using the current byte aligns
        /// </summary>
        /// <returns></returns>
        private uint ToUint(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToUInt32(data, 0);
        }

        /// <summary>
        /// Converts 4 bytes to an int using the current byte aligns
        /// </summary>
        /// <returns></returns>
        private int ToInt(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToInt32(data, 0);
        }

        private double ToDouble(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToDouble(data, 0);
        }

        private float ToSingle(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToSingle(data, 0);
        }

        private short ToShort(byte[] data)
        {
            if (isLittleEndian != BitConverter.IsLittleEndian)
                Array.Reverse(data);

            return BitConverter.ToInt16(data, 0);
        }

        private sbyte ToSByte(byte[] data)
        {
            // An sbyte should just be a byte with an offset range.
            return (sbyte)(data[0] - byte.MaxValue);
        }

        /// <summary>
        /// Retrieves an array from a byte array using the supplied converter
        /// to read each individual element from the supplied byte array
        /// </summary>
        /// <param name="data"></param>
        /// <param name="elementLengthBytes"></param>
        /// <param name="converter"></param>
        /// <returns></returns>
        private Array GetArray<T>(byte[] data, int elementLengthBytes, ConverterMethod<T> converter)
        {
            Array convertedData = Array.CreateInstance(typeof(T), data.Length / elementLengthBytes);

            var buffer = new byte[elementLengthBytes];

            // Read each element from the array
            for (int elementCount = 0; elementCount < data.Length / elementLengthBytes; elementCount++)
            {
                // Place the data for the current element into the buffer
                Array.Copy(data, elementCount * elementLengthBytes, buffer, 0, elementLengthBytes);

                // Process the data and place it into the output array
                convertedData.SetValue(converter(buffer), elementCount);
            }

            return convertedData;
        }

        /// <summary>
        /// A delegate used to invoke any of the data conversion methods
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private delegate T ConverterMethod<out T>(byte[] data);

        #endregion

        #region Stream seek methods - used to get to locations within the JPEG

        /// <summary>
        /// Scans to the Exif block
        /// </summary>
        private void ReadToExifStart()
        {
            // The file has a number of blocks (Exif/JFIF), each of which
            // has a tag number followed by a length. We scan the document until the required tag (0xFFE1)
            // is found. All tags start with FF, so a non FF tag indicates an error.

            // Get the next tag.
            byte markerStart;
            byte markerNumber = 0;
            while (((markerStart = reader.ReadByte()) == 0xFF) && (markerNumber = reader.ReadByte()) != 0xE1)
            {
                // Get the length of the data.
                ushort dataLength = ReadUShort();

                // Jump to the end of the data (note that the size field includes its own size)!
                reader.BaseStream.Seek(dataLength - 2, SeekOrigin.Current);
            }

            // It's only success if we found the 0xFFE1 marker
            if (markerStart != 0xFF || markerNumber != 0xE1)
                throw new Exception("Could not find Exif data block");
        }

        /// <summary>
        /// Reads through the Exif data and builds an index of all Exif tags in the document
        /// </summary>
        /// <returns></returns>
        private void CreateTagIndex()
        {
            // The next 4 bytes are the size of the Exif data.
            ReadUShort();

            // Next is the Exif data itself. It starts with the ASCII "Exif" followed by 2 zero bytes.
            if (ReadString(4) != "Exif")
                throw new Exception("Exif data not found");

            // 2 zero bytes
            if (ReadUShort() != 0)
                throw new Exception("Malformed Exif data");

            // We're now into the TIFF format
            tiffHeaderStart = reader.BaseStream.Position;

            // What byte align will be used for the TIFF part of the document? II for Intel, MM for Motorola
            isLittleEndian = ReadString(2) == "II";

            // Next 2 bytes are always the same.
            if (ReadUShort() != 0x002A)
                throw new Exception("Error in TIFF data");

            // Get the offset to the IFD (image file directory)
            uint ifdOffset = ReadUint();

            // Note that this offset is from the first byte of the TIFF header. Jump to the IFD.
            fileStream.Position = ifdOffset + tiffHeaderStart;

            // Catalogue this first IFD (there will be another IFD)
            CatalogueIFD();

            // There's more data stored in the subifd, the offset to which is found in tag 0x8769.
            // As with all TIFF offsets, it will be relative to the first byte of the TIFF header.
            uint offset;
            if (!GetTagValue(0x8769, out offset))
                throw new Exception("Unable to locate Exif data");

            // Jump to the exif SubIFD
            fileStream.Position = offset + tiffHeaderStart;

            // Add the subIFD to the catalogue too
            CatalogueIFD();

            // Go to the GPS IFD and catalogue that too. It's an optional
            // section.
            if (GetTagValue(0x8825, out offset))
            {
                // Jump to the GPS SubIFD
                fileStream.Position = offset + tiffHeaderStart;

                // Add the subIFD to the catalogue too
                CatalogueIFD();
            }
        }

        #endregion

        #region Exif data catalog and retrieval methods

        public bool GetTagValue<T>(ExifTags tag, out T result)
        {
            return GetTagValue((ushort)tag, out result);
        }

        /// <summary>
        /// Retrieves an Exif value with the requested tag ID
        /// </summary>
        /// <param name="tagID"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        public bool GetTagValue<T>(ushort tagID, out T result)
        {
            ushort tiffDataType;
            uint numberOfComponents;
            byte[] tagData = GetTagBytes(tagID, out tiffDataType, out numberOfComponents);

            if (tagData == null)
            {
                result = default(T);
                return false;
            }

            byte fieldLength = GetTIFFFieldLength(tiffDataType);

            // Convert the data to the appropriate datatype. Note the weird boxing via object.
            // The compiler doesn't like it otherwise.
            switch (tiffDataType)
            {
                case 1:
                    // unsigned byte
                    if (numberOfComponents == 1)
                        result = (T)(object)tagData[0];
                    else
                        result = (T)(object)tagData;
                    return true;
                case 2:
                    // ascii string
                    string str = Encoding.ASCII.GetString(tagData);

                    // There may be a null character within the string
                    int nullCharIndex = str.IndexOf('\0');
                    if (nullCharIndex != -1)
                        str = str.Substring(0, nullCharIndex);

                    // Special processing for dates.
                    if (typeof(T) == typeof(DateTime))
                    {
                        result =
                            (T)(object)DateTime.ParseExact(str, "yyyy:MM:dd HH:mm:ss", CultureInfo.InvariantCulture);
                        return true;
                    }

                    result = (T)(object)str;
                    return true;
                case 3:
                    // unsigned short
                    if (numberOfComponents == 1)
                        result = (T)(object)ToUShort(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToUShort);
                    return true;
                case 4:
                    // unsigned long
                    if (numberOfComponents == 1)
                        result = (T)(object)ToUint(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToUint);
                    return true;
                case 5:
                    // unsigned rational
                    if (numberOfComponents == 1)
                        result = (T)(object)ToURational(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToURational);
                    return true;
                case 6:
                    // signed byte
                    if (numberOfComponents == 1)
                        result = (T)(object)ToSByte(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToSByte);
                    return true;
                case 7:
                    // undefined. Treat it as an unsigned integer.
                    if (numberOfComponents == 1)
                        result = (T)(object)ToUint(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToUint);
                    return true;
                case 8:
                    // Signed short
                    if (numberOfComponents == 1)
                        result = (T)(object)ToShort(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToShort);
                    return true;
                case 9:
                    // Signed long
                    if (numberOfComponents == 1)
                        result = (T)(object)ToInt(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToInt);
                    return true;
                case 10:
                    // signed rational
                    if (numberOfComponents == 1)
                        result = (T)(object)ToRational(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToRational);
                    return true;
                case 11:
                    // single float
                    if (numberOfComponents == 1)
                        result = (T)(object)ToSingle(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToSingle);
                    return true;
                case 12:
                    // double float
                    if (numberOfComponents == 1)
                        result = (T)(object)ToDouble(tagData);
                    else
                        result = (T)(object)GetArray(tagData, fieldLength, ToDouble);
                    return true;
                default:
                    throw new Exception(string.Format("Unknown TIFF datatype: {0}", tiffDataType));
            }
        }

        /// <summary>
        /// Gets the data in the specified tag ID, starting from before the IFD block.
        /// </summary>
        /// <param name="tiffDataType"></param>
        /// <param name="numberOfComponents">The number of items which make up the data item - i.e. for a string, this will be the
        /// number of characters in the string</param>
        /// <param name="tagID"></param>
        private byte[] GetTagBytes(ushort tagID, out ushort tiffDataType, out uint numberOfComponents)
        {
            // Get the tag's offset from the catalogue and do some basic error checks
            if (fileStream == null || reader == null || catalogue == null || !catalogue.ContainsKey(tagID))
            {
                tiffDataType = 0;
                numberOfComponents = 0;
                return null;
            }

            long tagOffset = catalogue[tagID];

            // Jump to the TIFF offset
            fileStream.Position = tagOffset;

            // Read the tag number from the file
            ushort currentTagID = ReadUShort();

            if (currentTagID != tagID)
                throw new Exception("Tag number not at expected offset");

            // Read the offset to the Exif IFD
            tiffDataType = ReadUShort();
            numberOfComponents = ReadUint();
            byte[] tagData = ReadBytes(4);

            // If the total space taken up by the field is longer than the
            // 2 bytes afforded by the tagData, tagData will contain an offset
            // to the actual data.
            var dataSize = (int)(numberOfComponents * GetTIFFFieldLength(tiffDataType));

            if (dataSize > 4)
            {
                ushort offsetAddress = ToUShort(tagData);
                return ReadBytes(offsetAddress, dataSize);
            }

            // The value is stored in the tagData starting from the left
            Array.Resize(ref tagData, dataSize);

            return tagData;
        }

        /// <summary>
        /// Records all Exif tags and their offsets within
        /// the file from the current IFD
        /// </summary>
        private void CatalogueIFD()
        {
            if (catalogue == null)
                catalogue = new Dictionary<ushort, long>();

            // Assume we're just before the IFD.

            // First 2 bytes is the number of entries in this IFD
            ushort entryCount = ReadUShort();

            for (ushort currentEntry = 0; currentEntry < entryCount; currentEntry++)
            {
                ushort currentTagNumber = ReadUShort();

                // Record this in the catalogue
                catalogue[currentTagNumber] = fileStream.Position - 2;

                // Go to the end of this item (10 bytes, as each entry is 12 bytes long)
                reader.BaseStream.Seek(10, SeekOrigin.Current);
            }
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // Make sure the file handle is released
            if (reader != null)
                reader.Close();
            if (fileStream != null)
                fileStream.Close();
        }

        #endregion
    }
}
