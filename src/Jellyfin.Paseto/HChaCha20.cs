using System;
using System.Buffers.Binary;

namespace Jellyfin.Paseto;

/// <summary>
/// HChaCha20 as defined by the XChaCha20 construction (draft-irtf-cfrg-xchacha),
/// used by PASETO v4 to derive a ChaCha20 subkey from a 32-byte key and the first
/// 16 bytes of the 24-byte nonce.
/// </summary>
internal static class HChaCha20
{
    /// <summary>
    /// Derives a 32-byte ChaCha20 subkey from a 32-byte key and 16-byte nonce.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 16-byte nonce.</param>
    /// <param name="output">The 32-byte output subkey.</param>
    /// <exception cref="ArgumentException">The key, nonce or output has the wrong length.</exception>
    public static void DeriveKey(ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> output)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("HChaCha20 key must be 32 bytes.", nameof(key));
        }

        if (nonce.Length != 16)
        {
            throw new ArgumentException("HChaCha20 nonce must be 16 bytes.", nameof(nonce));
        }

        if (output.Length != 32)
        {
            throw new ArgumentException("HChaCha20 output must be 32 bytes.", nameof(output));
        }

        uint[] state = new uint[16];
        state[0] = 0x61707865;
        state[1] = 0x3320646e;
        state[2] = 0x79622d32;
        state[3] = 0x6b206574;
        for (int i = 0; i < 8; i++)
        {
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(key.Slice(i * 4, 4));
        }

        for (int i = 0; i < 4; i++)
        {
            state[12 + i] = BinaryPrimitives.ReadUInt32LittleEndian(nonce.Slice(i * 4, 4));
        }

        for (int round = 0; round < 10; round++)
        {
            DoubleRound(state);
        }

        Span<byte> tmp = stackalloc byte[32];
        for (int i = 0; i < 4; i++)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(tmp.Slice(i * 4, 4), state[i]);
            BinaryPrimitives.WriteUInt32LittleEndian(tmp.Slice(16 + (i * 4), 4), state[12 + i]);
        }

        tmp.CopyTo(output);
    }

    /// <summary>
    /// One double round (column round followed by diagonal round) of ChaCha on the 4x4 matrix.
    /// </summary>
    /// <param name="state">The 16-word state, modified in place.</param>
    private static void DoubleRound(uint[] state)
    {
        QuarterRound(state, 0, 4, 8, 12);
        QuarterRound(state, 1, 5, 9, 13);
        QuarterRound(state, 2, 6, 10, 14);
        QuarterRound(state, 3, 7, 11, 15);
        QuarterRound(state, 0, 5, 10, 15);
        QuarterRound(state, 1, 6, 11, 12);
        QuarterRound(state, 2, 7, 8, 13);
        QuarterRound(state, 3, 4, 9, 14);
    }

    /// <summary>
    /// The ChaCha quarter-round applied to four state words.
    /// </summary>
    private static void QuarterRound(uint[] state, int a, int b, int c, int d)
    {
        state[a] += state[b];
        state[d] = Rotl32(state[d] ^ state[a], 16);
        state[c] += state[d];
        state[b] = Rotl32(state[b] ^ state[c], 12);
        state[a] += state[b];
        state[d] = Rotl32(state[d] ^ state[a], 8);
        state[c] += state[d];
        state[b] = Rotl32(state[b] ^ state[c], 7);
    }

    /// <summary>
    /// Rotates a 32-bit value left by the given number of bits.
    /// </summary>
    private static uint Rotl32(uint value, int bits) => (value << bits) | (value >> (32 - bits));
}
