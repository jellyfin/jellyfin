using System;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Jellyfin.Database.Providers.SqlServer;

/// <summary>
/// Stores a <see cref="Guid"/> as 16 bytes in the order of its canonical text.
/// </summary>
/// <remarks>
/// SQL Server orders <c>uniqueidentifier</c> by the last six bytes first, so the smallest id of a group and the order
/// of a list of ids would differ from those of the other engines, which order ids as their text does. <c>binary(16)</c>
/// orders bytes from the first, and with the bytes in the order of the text the database orders ids, and picks the
/// smallest, the way the text orders them.
/// </remarks>
internal sealed class BigEndianGuidConverter : ValueConverter<Guid, byte[]>
{
    /// <summary>
    /// The type of the column of an id.
    /// </summary>
    internal const string ColumnType = "binary(16)";

    /// <summary>
    /// Initializes a new instance of the <see cref="BigEndianGuidConverter"/> class.
    /// </summary>
    public BigEndianGuidConverter()
        : base(value => ToBytes(value), bytes => FromBytes(bytes))
    {
    }

    /// <summary>
    /// Gets the bytes of an id in the order of its text.
    /// </summary>
    /// <param name="value">The id.</param>
    /// <returns>The 16 bytes.</returns>
    internal static byte[] ToBytes(Guid value) => value.ToByteArray(bigEndian: true);

    /// <summary>
    /// Gets the id of 16 bytes in the order of its text.
    /// </summary>
    /// <param name="value">The 16 bytes.</param>
    /// <returns>The id.</returns>
    internal static Guid FromBytes(byte[] value) => new(value, bigEndian: true);
}
