using System.Buffers.Binary;
using System.IO;

namespace DvdLib;

/// <summary>
/// A reader for big endian binary data.
/// </summary>
public class BigEndianBinaryReader : BinaryReader
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BigEndianBinaryReader"/> class.
    /// </summary>
    /// <param name="input">The input data stream.</param>
    /// <returns>The <see cref="BigEndianBinaryReader"/>.</returns>
    public BigEndianBinaryReader(Stream input)
        : base(input)
    {
    }

    /// <summary>
    /// Reads a unsigned 16 bit integer from the binary data stream.
    /// </summary>
    /// <returns>The read unsigned 16 bit integer.</returns>
    public override ushort ReadUInt16()
    {
        return BinaryPrimitives.ReadUInt16BigEndian(base.ReadBytes(2));
    }

    /// <summary>
    /// Reads a unsigned 32 bit integer from the binary data stream.
    /// </summary>
    /// <returns>The read unsigned 32 bit integer.</returns>
    public override uint ReadUInt32()
    {
        return BinaryPrimitives.ReadUInt32BigEndian(base.ReadBytes(4));
    }
}
