using System;
using System.Buffers.Binary;

namespace Jellyfin.Paseto;

/// <summary>
/// In-house implementation of the BLAKE2b hash (RFC 7693) with keyed mode and
/// configurable digest size (1..64 bytes), as required by PASETO v4.
/// </summary>
internal static class KeyedBlake2b
{
    private static readonly ulong[] IV =
    {
        0x6a09e667f3bcc908UL, 0xbb67ae8584caa73bUL, 0x3c6ef372fe94f82bUL, 0xa54ff53a5f1d36f1UL,
        0x510e527fade682d1UL, 0x9b05688c2b3e6c1fUL, 0x1f83d9abfb41bd6bUL, 0x5be0cd19137e2179UL
    };

    private static readonly byte[] Sigma =
    {
        0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15,
        14, 10, 4, 8, 9, 15, 13, 6, 1, 12, 0, 2, 11, 7, 5, 3,
        11, 8, 12, 0, 5, 2, 15, 13, 10, 14, 3, 6, 7, 1, 9, 4,
        7, 9, 3, 1, 13, 12, 11, 14, 2, 6, 5, 10, 4, 0, 15, 8,
        9, 0, 5, 7, 2, 4, 10, 15, 14, 1, 11, 12, 6, 8, 3, 13,
        2, 12, 6, 10, 0, 11, 8, 3, 4, 13, 7, 5, 15, 14, 1, 9,
        12, 5, 1, 15, 14, 13, 4, 10, 0, 7, 6, 3, 9, 2, 8, 11,
        13, 11, 7, 14, 12, 1, 3, 9, 5, 0, 15, 4, 8, 6, 2, 10,
        6, 15, 14, 9, 11, 3, 0, 8, 12, 2, 13, 7, 1, 4, 10, 5,
        10, 2, 8, 4, 7, 6, 1, 5, 15, 11, 9, 14, 3, 12, 13, 0
    };

    /// <summary>
    /// Computes a keyed (or unkeyed if the key is empty) BLAKE2b digest.
    /// </summary>
    /// <param name="key">The key (0..64 bytes); an empty key selects raw BLAKE2b.</param>
    /// <param name="message">The message to hash.</param>
    /// <param name="outputLength">The digest length in bytes (1..64).</param>
    /// <returns>The digest.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputLength"/> is outside 1..64.</exception>
    /// <exception cref="ArgumentException"><paramref name="key"/> is longer than 64 bytes.</exception>
    public static byte[] Hash(ReadOnlySpan<byte> key, ReadOnlySpan<byte> message, int outputLength)
    {
        if (outputLength is < 1 or > 64)
        {
            throw new ArgumentOutOfRangeException(nameof(outputLength));
        }

        if (key.Length > 64)
        {
            throw new ArgumentException("Key must be at most 64 bytes.", nameof(key));
        }

        Span<byte> param = stackalloc byte[64];
        param[0] = (byte)outputLength;
        param[1] = (byte)key.Length;
        param[2] = 1; // fanout
        param[3] = 1; // depth

        ulong[] h = new ulong[8];
        for (int i = 0; i < 8; i++)
        {
            h[i] = IV[i] ^ BinaryPrimitives.ReadUInt64LittleEndian(param.Slice(i * 8, 8));
        }

        ulong counter = 0;
        byte[] block = new byte[128];
        bool hasKey = key.Length > 0;

        void Compress(ReadOnlySpan<byte> data, bool isFinal)
        {
            ulong[] v = new ulong[16];
            for (int i = 0; i < 8; i++)
            {
                v[i] = h[i];
                v[i + 8] = IV[i];
            }

            v[12] ^= counter;
            v[14] ^= isFinal ? ulong.MaxValue : 0;

            ulong[] m = new ulong[16];
            for (int i = 0; i < 16; i++)
            {
                m[i] = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(i * 8, 8));
            }

            for (int round = 0; round < 12; round++)
            {
                int s = (round % 10) * 16;
                G(v, 0, 4, 8, 12, m[Sigma[s]], m[Sigma[s + 1]]);
                G(v, 1, 5, 9, 13, m[Sigma[s + 2]], m[Sigma[s + 3]]);
                G(v, 2, 6, 10, 14, m[Sigma[s + 4]], m[Sigma[s + 5]]);
                G(v, 3, 7, 11, 15, m[Sigma[s + 6]], m[Sigma[s + 7]]);
                G(v, 0, 5, 10, 15, m[Sigma[s + 8]], m[Sigma[s + 9]]);
                G(v, 1, 6, 11, 12, m[Sigma[s + 10]], m[Sigma[s + 11]]);
                G(v, 2, 7, 8, 13, m[Sigma[s + 12]], m[Sigma[s + 13]]);
                G(v, 3, 4, 9, 14, m[Sigma[s + 14]], m[Sigma[s + 15]]);
            }

            for (int i = 0; i < 8; i++)
            {
                h[i] ^= v[i] ^ v[i + 8];
            }
        }

        if (hasKey)
        {
            key.CopyTo(block);
            counter += 128;
            Compress(block, message.Length == 0);
            Array.Clear(block, 0, block.Length);
        }

        int offset = 0;
        while (offset + 128 <= message.Length)
        {
            counter += 128;
            Compress(message.Slice(offset, 128), false);
            offset += 128;
        }

        int remaining = message.Length - offset;
        if (remaining > 0)
        {
            counter += (ulong)remaining;
            message.Slice(offset, remaining).CopyTo(block);
            Compress(block, true);
        }
        else if (message.Length > 0 || !hasKey)
        {
            // Input is an exact multiple of the block size (or empty and unkeyed):
            // a final, empty block is required by RFC 7693.
            Compress(block, true);
        }

        Span<byte> full = stackalloc byte[64];
        for (int i = 0; i < 8; i++)
        {
            BinaryPrimitives.WriteUInt64LittleEndian(full.Slice(i * 8, 8), h[i]);
        }

        var output = new byte[outputLength];
        full[..outputLength].CopyTo(output);
        return output;
    }

    /// <summary>
    /// The BLAKE2b mixing function applied to two message words.
    /// </summary>
    private static void G(ulong[] v, int a, int b, int c, int d, ulong x, ulong y)
    {
        v[a] += v[b] + x;
        v[d] = Rotr64(v[d] ^ v[a], 32);
        v[c] += v[d];
        v[b] = Rotr64(v[b] ^ v[c], 24);
        v[a] += v[b] + y;
        v[d] = Rotr64(v[d] ^ v[a], 16);
        v[c] += v[d];
        v[b] = Rotr64(v[b] ^ v[c], 63);
    }

    /// <summary>
    /// Rotates a 64-bit value right by the given number of bits.
    /// </summary>
    private static ulong Rotr64(ulong value, int bits) => (value >> bits) | (value << (64 - bits));
}
