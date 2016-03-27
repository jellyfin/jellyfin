using MediaBrowser.Model.Drawing;
using MediaBrowser.Model.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommonIO;

namespace Emby.Drawing.Common
{
    /// <summary>
    /// Taken from http://stackoverflow.com/questions/111345/getting-image-dimensions-without-reading-the-entire-file/111349
    /// http://www.codeproject.com/Articles/35978/Reading-Image-Headers-to-Get-Width-and-Height
    /// Minor improvements including supporting unsigned 16-bit integers when decoding Jfif and added logic
    /// to load the image using new Bitmap if reading the headers fails
    /// </summary>
    public static class ImageHeader
    {
        /// <summary>
        /// The error message
        /// </summary>
        const string ErrorMessage = "Could not recognize image format.";

        /// <summary>
        /// The image format decoders
        /// </summary>
        private static readonly KeyValuePair<byte[], Func<BinaryReader, ImageSize>>[] ImageFormatDecoders = new Dictionary<byte[], Func<BinaryReader, ImageSize>>
        { 
            { new byte[] { 0x42, 0x4D }, DecodeBitmap }, 
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x37, 0x61 }, DecodeGif }, 
            { new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, DecodeGif }, 
            { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, DecodePng },
            { new byte[] { 0xff, 0xd8 }, DecodeJfif }

        }.ToArray();

        private static readonly int MaxMagicBytesLength = ImageFormatDecoders.Select(i => i.Key.Length).OrderByDescending(i => i).First();

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="path">The path of the image to get the dimensions of.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="fileSystem">The file system.</param>
        /// <returns>The dimensions of the specified image.</returns>
        /// <exception cref="ArgumentException">The image was of an unrecognised format.</exception>
        public static ImageSize GetDimensions(string path, ILogger logger, IFileSystem fileSystem)
        {
            using (var fs = File.OpenRead(path))
            {
                using (var binaryReader = new BinaryReader(fs))
                {
                    return GetDimensions(binaryReader);
                }
            }
        }

        /// <summary>
        /// Gets the dimensions of an image.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>Size.</returns>
        /// <exception cref="System.ArgumentException">binaryReader</exception>
        /// <exception cref="ArgumentException">The image was of an unrecognized format.</exception>
        private static ImageSize GetDimensions(BinaryReader binaryReader)
        {
            var magicBytes = new byte[MaxMagicBytesLength];

            for (var i = 0; i < MaxMagicBytesLength; i += 1)
            {
                magicBytes[i] = binaryReader.ReadByte();

                foreach (var kvPair in ImageFormatDecoders)
                {
                    if (StartsWith(magicBytes, kvPair.Key))
                    {
                        return kvPair.Value(binaryReader);
                    }
                }
            }

            throw new ArgumentException(ErrorMessage, "binaryReader");
        }

        /// <summary>
        /// Startses the with.
        /// </summary>
        /// <param name="thisBytes">The this bytes.</param>
        /// <param name="thatBytes">The that bytes.</param>
        /// <returns><c>true</c> if XXXX, <c>false</c> otherwise</returns>
        private static bool StartsWith(byte[] thisBytes, byte[] thatBytes)
        {
            for (int i = 0; i < thatBytes.Length; i += 1)
            {
                if (thisBytes[i] != thatBytes[i])
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Reads the little endian int16.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Int16.</returns>
        private static short ReadLittleEndianInt16(BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(short)];

            for (int i = 0; i < sizeof(short); i += 1)
            {
                bytes[sizeof(short) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt16(bytes, 0);
        }

        /// <summary>
        /// Reads the little endian int32.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>System.Int32.</returns>
        private static int ReadLittleEndianInt32(BinaryReader binaryReader)
        {
            var bytes = new byte[sizeof(int)];
            for (int i = 0; i < sizeof(int); i += 1)
            {
                bytes[sizeof(int) - 1 - i] = binaryReader.ReadByte();
            }
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// Decodes the bitmap.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>Size.</returns>
        private static ImageSize DecodeBitmap(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(16);
            int width = binaryReader.ReadInt32();
            int height = binaryReader.ReadInt32();
            return new ImageSize
            {
                Width = width,
                Height = height
            };
        }

        /// <summary>
        /// Decodes the GIF.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>Size.</returns>
        private static ImageSize DecodeGif(BinaryReader binaryReader)
        {
            int width = binaryReader.ReadInt16();
            int height = binaryReader.ReadInt16();
            return new ImageSize
            {
                Width = width,
                Height = height
            };
        }

        /// <summary>
        /// Decodes the PNG.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>Size.</returns>
        private static ImageSize DecodePng(BinaryReader binaryReader)
        {
            binaryReader.ReadBytes(8);
            int width = ReadLittleEndianInt32(binaryReader);
            int height = ReadLittleEndianInt32(binaryReader);
            return new ImageSize
            {
                Width = width,
                Height = height
            };
        }

        /// <summary>
        /// Decodes the jfif.
        /// </summary>
        /// <param name="binaryReader">The binary reader.</param>
        /// <returns>Size.</returns>
        /// <exception cref="System.ArgumentException"></exception>
        private static ImageSize DecodeJfif(BinaryReader binaryReader)
        {
            while (binaryReader.ReadByte() == 0xff)
            {
                byte marker = binaryReader.ReadByte();
                short chunkLength = ReadLittleEndianInt16(binaryReader);
                if (marker == 0xc0)
                {
                    binaryReader.ReadByte();
                    int height = ReadLittleEndianInt16(binaryReader);
                    int width = ReadLittleEndianInt16(binaryReader);
                    return new ImageSize
                    {
                        Width = width,
                        Height = height
                    };
                }

                if (chunkLength < 0)
                {
                    var uchunkLength = (ushort)chunkLength;
                    binaryReader.ReadBytes(uchunkLength - 2);
                }
                else
                {
                    binaryReader.ReadBytes(chunkLength - 2);
                }
            }

            throw new ArgumentException(ErrorMessage);
        }
    }
}