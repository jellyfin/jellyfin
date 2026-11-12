using System;
using System.Buffers.Binary;

namespace Jellyfin.Paseto;

/// <summary>
/// Pre-Authentication Encoding (PAE) as defined by the PASETO specification.
/// LE64-frames the piece count and each piece length, clearing the most significant
/// bit of every length for interoperability.
/// </summary>
internal static class Pae
{
    /// <summary>
    /// Encodes zero pieces using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <returns>The PAE encoding, which is always 8 zero bytes.</returns>
    public static byte[] Encode() => EncodeCore(0, default, default, default, default, default);

    /// <summary>
    /// Encodes one byte piece using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <param name="piece1">The first piece.</param>
    /// <returns>The PAE encoding.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> piece1) => EncodeCore(1, piece1, default, default, default, default);

    /// <summary>
    /// Encodes two byte pieces using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <param name="piece1">The first piece.</param>
    /// <param name="piece2">The second piece.</param>
    /// <returns>The PAE encoding.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> piece1, ReadOnlySpan<byte> piece2) => EncodeCore(2, piece1, piece2, default, default, default);

    /// <summary>
    /// Encodes three byte pieces using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <param name="piece1">The first piece.</param>
    /// <param name="piece2">The second piece.</param>
    /// <param name="piece3">The third piece.</param>
    /// <returns>The PAE encoding.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> piece1, ReadOnlySpan<byte> piece2, ReadOnlySpan<byte> piece3) => EncodeCore(3, piece1, piece2, piece3, default, default);

    /// <summary>
    /// Encodes four byte pieces using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <param name="piece1">The first piece.</param>
    /// <param name="piece2">The second piece.</param>
    /// <param name="piece3">The third piece.</param>
    /// <param name="piece4">The fourth piece.</param>
    /// <returns>The PAE encoding.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> piece1, ReadOnlySpan<byte> piece2, ReadOnlySpan<byte> piece3, ReadOnlySpan<byte> piece4) => EncodeCore(4, piece1, piece2, piece3, piece4, default);

    /// <summary>
    /// Encodes five byte pieces using the PASETO Pre-Authentication Encoding.
    /// </summary>
    /// <param name="piece1">The first piece.</param>
    /// <param name="piece2">The second piece.</param>
    /// <param name="piece3">The third piece.</param>
    /// <param name="piece4">The fourth piece.</param>
    /// <param name="piece5">The fifth piece.</param>
    /// <returns>The PAE encoding.</returns>
    public static byte[] Encode(ReadOnlySpan<byte> piece1, ReadOnlySpan<byte> piece2, ReadOnlySpan<byte> piece3, ReadOnlySpan<byte> piece4, ReadOnlySpan<byte> piece5) => EncodeCore(5, piece1, piece2, piece3, piece4, piece5);

    /// <summary>
    /// Builds the PAE output for the given number of leading pieces.
    /// </summary>
    private static byte[] EncodeCore(int pieceCount, ReadOnlySpan<byte> piece1, ReadOnlySpan<byte> piece2, ReadOnlySpan<byte> piece3, ReadOnlySpan<byte> piece4, ReadOnlySpan<byte> piece5)
    {
        long totalLength = 8;
        totalLength += 8L + piece1.Length;
        totalLength += 8L + piece2.Length;
        totalLength += 8L + piece3.Length;
        totalLength += 8L + piece4.Length;
        totalLength += 8L + piece5.Length;

        var output = new byte[totalLength];
        Span<byte> cursor = output;
        WriteLe64(ref cursor, (ulong)pieceCount);
        WritePiece(ref cursor, piece1);
        WritePiece(ref cursor, piece2);
        WritePiece(ref cursor, piece3);
        WritePiece(ref cursor, piece4);
        WritePiece(ref cursor, piece5);
        return output;
    }

    /// <summary>
    /// Appends a single piece to the PAE output.
    /// </summary>
    /// <param name="destination">The destination span; advanced past the piece.</param>
    /// <param name="piece">The piece to append.</param>
    private static void WritePiece(ref Span<byte> destination, ReadOnlySpan<byte> piece)
    {
        WriteLe64(ref destination, (ulong)piece.Length);
        piece.CopyTo(destination);
        destination = destination[piece.Length..];
    }

    /// <summary>
    /// Writes a 64-bit little-endian length with the most significant bit cleared.
    /// </summary>
    /// <param name="destination">The destination span; advanced by 8 bytes.</param>
    /// <param name="value">The length to write.</param>
    private static void WriteLe64(ref Span<byte> destination, ulong value)
    {
        BinaryPrimitives.WriteUInt64LittleEndian(destination, value & 0x7FFF_FFFF_FFFF_FFFFUL);
        destination = destination[8..];
    }
}
