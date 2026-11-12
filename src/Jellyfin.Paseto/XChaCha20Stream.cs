using System;
using System.Buffers.Binary;

namespace Jellyfin.Paseto;

/// <summary>
/// The XChaCha20 stream cipher (HChaCha20 subkey derivation plus ChaCha20 with a
/// 32-bit block counter starting at zero), as used by PASETO v4.local.
/// Encryption and decryption are the same operation (stream XOR).
/// </summary>
internal static class XChaCha20Stream
{
    /// <summary>
    /// XORs the input with the XChaCha20 keystream derived from the key and 24-byte nonce.
    /// </summary>
    /// <param name="input">The input bytes.</param>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="nonce">The 24-byte nonce.</param>
    /// <param name="output">The output span of the same length as the input.</param>
    /// <exception cref="ArgumentException">The key, nonce or output has the wrong length.</exception>
    public static void Xor(ReadOnlySpan<byte> input, ReadOnlySpan<byte> key, ReadOnlySpan<byte> nonce, Span<byte> output)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("XChaCha20 key must be 32 bytes.", nameof(key));
        }

        if (nonce.Length != 24)
        {
            throw new ArgumentException("XChaCha20 nonce must be 24 bytes.", nameof(nonce));
        }

        if (output.Length != input.Length)
        {
            throw new ArgumentException("Output must have the same length as the input.", nameof(output));
        }

        Span<byte> subkey = stackalloc byte[32];
        HChaCha20.DeriveKey(key, nonce[..16], subkey);

        uint nonceA = BinaryPrimitives.ReadUInt32LittleEndian(nonce[16..20]);
        uint nonceB = BinaryPrimitives.ReadUInt32LittleEndian(nonce[20..24]);

        byte[] block = new byte[64];
        uint counter = 0;
        int offset = 0;
        while (offset < input.Length)
        {
            GenerateBlock(subkey, counter, nonceA, nonceB, block);
            int chunk = Math.Min(64, input.Length - offset);
            for (int i = 0; i < chunk; i++)
            {
                output[offset + i] = (byte)(input[offset + i] ^ block[i]);
            }

            counter++;
            offset += chunk;
        }
    }

    /// <summary>
    /// Generates one 64-byte ChaCha20 keystream block.
    /// </summary>
    /// <param name="subkey">The 32-byte subkey produced by HChaCha20.</param>
    /// <param name="counter">The 32-bit block counter.</param>
    /// <param name="nonceA">The first 4 bytes of the 8-byte nonce tail.</param>
    /// <param name="nonceB">The last 4 bytes of the 8-byte nonce tail.</param>
    /// <param name="block">The 64-byte output block.</param>
    private static void GenerateBlock(ReadOnlySpan<byte> subkey, uint counter, uint nonceA, uint nonceB, Span<byte> block)
    {
        uint[] state = new uint[16];
        state[0] = 0x61707865;
        state[1] = 0x3320646e;
        state[2] = 0x79622d32;
        state[3] = 0x6b206574;
        for (int i = 0; i < 8; i++)
        {
            state[4 + i] = BinaryPrimitives.ReadUInt32LittleEndian(subkey.Slice(i * 4, 4));
        }

        state[12] = counter;
        state[13] = 0;
        state[14] = nonceA;
        state[15] = nonceB;

        uint[] working = new uint[16];
        Array.Copy(state, working, 16);
        for (int round = 0; round < 10; round++)
        {
            DoubleRound(working);
        }

        for (int i = 0; i < 16; i++)
        {
            working[i] += state[i];
            BinaryPrimitives.WriteUInt32LittleEndian(block.Slice(i * 4, 4), working[i]);
        }
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
