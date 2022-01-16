using System;
using System.Buffers.Binary;
using Jellyfin.MediaEncoding.Keyframes.Matroska.Models;
using NEbml.Core;

namespace Jellyfin.MediaEncoding.Keyframes.Matroska.Extensions;

/// <summary>
/// Extension methods for the <see cref="EbmlReader"/> class.
/// </summary>
internal static class EbmlReaderExtensions
{
    /// <summary>
    /// Traverses the current container to find the element with <paramref name="identifier"/> identifier.
    /// </summary>
    /// <param name="reader">An instance of <see cref="EbmlReader"/>.</param>
    /// <param name="identifier">The element identifier.</param>
    /// <returns>A value indicating whether the element was found.</returns>
    internal static bool FindElement(this EbmlReader reader, ulong identifier)
    {
        while (reader.ReadNext())
        {
            if (reader.ElementId.EncodedValue == identifier)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Reads the current position in the file as an unsigned integer converted from binary.
    /// </summary>
    /// <param name="reader">An instance of <see cref="EbmlReader"/>.</param>
    /// <returns>The unsigned integer.</returns>
    internal static uint ReadUIntFromBinary(this EbmlReader reader)
    {
        var buffer = new byte[4];
        reader.ReadBinary(buffer, 0, 4);
        return BinaryPrimitives.ReadUInt32BigEndian(buffer);
    }

    /// <summary>
    /// Reads from the start of the file to retrieve the SeekHead segment.
    /// </summary>
    /// <param name="reader">An instance of <see cref="EbmlReader"/>.</param>
    /// <returns>Instance of <see cref="SeekHead"/>.</returns>
    internal static SeekHead ReadSeekHead(this EbmlReader reader)
    {
        reader = reader ?? throw new ArgumentNullException(nameof(reader));

        if (reader.ElementPosition != 0)
        {
            throw new InvalidOperationException("File position must be at 0");
        }

        // Skip the header
        if (!reader.FindElement(MatroskaConstants.SegmentContainer))
        {
            throw new InvalidOperationException("Expected a segment container");
        }

        reader.EnterContainer();

        long? tracksPosition = null;
        long? cuesPosition = null;
        long? infoPosition = null;
        // The first element should be a SeekHead otherwise we'll have to search manually
        if (!reader.FindElement(MatroskaConstants.SeekHead))
        {
            throw new InvalidOperationException("Expected a SeekHead");
        }

        reader.EnterContainer();
        while (reader.FindElement(MatroskaConstants.Seek))
        {
            reader.EnterContainer();
            reader.ReadNext();
            var type = (ulong)reader.ReadUIntFromBinary();
            switch (type)
            {
                case MatroskaConstants.Tracks:
                    reader.ReadNext();
                    tracksPosition = (long)reader.ReadUInt();
                    break;
                case MatroskaConstants.Cues:
                    reader.ReadNext();
                    cuesPosition = (long)reader.ReadUInt();
                    break;
                case MatroskaConstants.Info:
                    reader.ReadNext();
                    infoPosition = (long)reader.ReadUInt();
                    break;
            }

            reader.LeaveContainer();

            if (tracksPosition.HasValue && cuesPosition.HasValue && infoPosition.HasValue)
            {
                break;
            }
        }

        reader.LeaveContainer();

        if (!tracksPosition.HasValue || !cuesPosition.HasValue || !infoPosition.HasValue)
        {
            throw new InvalidOperationException("SeekHead is missing or does not contain Info, Tracks and Cues positions. SeekHead referencing another SeekHead is not supported");
        }

        return new SeekHead(infoPosition.Value, tracksPosition.Value, cuesPosition.Value);
    }

    /// <summary>
    /// Reads from SegmentContainer to retrieve the Info segment.
    /// </summary>
    /// <param name="reader">An instance of <see cref="EbmlReader"/>.</param>
    /// <param name="position">The position of the info segment relative to the Segment container.</param>
    /// <returns>Instance of <see cref="Info"/>.</returns>
    internal static Info ReadInfo(this EbmlReader reader, long position)
    {
        reader.ReadAt(position);

        double? duration = null;
        reader.EnterContainer();
        // Mandatory element
        reader.FindElement(MatroskaConstants.TimestampScale);
        var timestampScale = reader.ReadUInt();

        if (reader.FindElement(MatroskaConstants.Duration))
        {
            duration = reader.ReadFloat();
        }

        reader.LeaveContainer();

        return new Info((long)timestampScale, duration);
    }

    /// <summary>
    /// Enters the Tracks segment and reads all tracks to find the specified type.
    /// </summary>
    /// <param name="reader">Instance of <see cref="EbmlReader"/>.</param>
    /// <param name="tracksPosition">The relative position of the tracks segment.</param>
    /// <param name="type">The track type identifier.</param>
    /// <returns>The first track number with the specified type.</returns>
    /// <exception cref="InvalidOperationException">Stream type is not found.</exception>
    internal static ulong FindFirstTrackNumberByType(this EbmlReader reader, long tracksPosition, ulong type)
    {
        reader.ReadAt(tracksPosition);

        reader.EnterContainer();
        while (reader.FindElement(MatroskaConstants.TrackEntry))
        {
            reader.EnterContainer();
            // Mandatory element
            reader.FindElement(MatroskaConstants.TrackNumber);
            var trackNumber = reader.ReadUInt();

            // Mandatory element
            reader.FindElement(MatroskaConstants.TrackType);
            var trackType = reader.ReadUInt();

            reader.LeaveContainer();
            if (trackType == MatroskaConstants.TrackTypeVideo)
            {
                reader.LeaveContainer();
                return trackNumber;
            }
        }

        reader.LeaveContainer();

        throw new InvalidOperationException($"No stream with type {type} found");
    }
}
