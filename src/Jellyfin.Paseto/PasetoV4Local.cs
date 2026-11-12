using System;
using System.Security.Cryptography;
using System.Text;

namespace Jellyfin.Paseto;

/// <summary>
/// PASETO v4.local (version 4, local purpose) authenticated encryption, implemented
/// to the paseto-standard specification: XChaCha20 stream encryption with a keyed
/// BLAKE2b-MAC over the PAE-encoded header, nonce, ciphertext, footer and implicit
/// assertion.
/// </summary>
public static class PasetoV4Local
{
    private const string Header = "v4.local.";

    private const int KeyLength = 32;

    private const int NonceLength = 32;

    private const int TagLength = 32;

    private const int MaxTokenLength = 1_000_000;

    private static readonly UTF8Encoding StrictUtf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>
    /// Encrypts and authenticates the given payload, returning a PASETO v4.local token.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="payload">The payload string.</param>
    /// <param name="footer">Optional footer string, authenticated but not encrypted.</param>
    /// <param name="implicitAssertion">Optional implicit assertion, authenticated but not transmitted.</param>
    /// <returns>The PASETO v4.local token.</returns>
    /// <exception cref="PasetoException"><paramref name="key"/> is not exactly 32 bytes, or <paramref name="payload"/> is null.</exception>
    public static string Encrypt(ReadOnlySpan<byte> key, string payload, string? footer = null, string? implicitAssertion = null)
    {
        if (key.Length != KeyLength)
        {
            throw new PasetoException("PASETO v4.local keys must be exactly 32 bytes.");
        }

        ArgumentNullException.ThrowIfNull(payload);

        Span<byte> nonce = stackalloc byte[NonceLength];
        RandomNumberGenerator.Fill(nonce);
        return EncryptCore(key, payload, nonce, footer, implicitAssertion);
    }

    /// <summary>
    /// Encrypts with an externally supplied nonce. Used by tests that must reproduce
    /// the official test vectors byte for byte.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="payload">The payload string.</param>
    /// <param name="nonce">The 32-byte nonce.</param>
    /// <param name="footer">Optional footer string.</param>
    /// <param name="implicitAssertion">Optional implicit assertion.</param>
    /// <returns>The PASETO v4.local token.</returns>
    /// <exception cref="PasetoException"><paramref name="key"/> is not exactly 32 bytes.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nonce"/> is not exactly 32 bytes.</exception>
    internal static string EncryptWithNonce(ReadOnlySpan<byte> key, string payload, ReadOnlySpan<byte> nonce, string? footer = null, string? implicitAssertion = null)
    {
        if (key.Length != KeyLength)
        {
            throw new PasetoException("PASETO v4.local keys must be exactly 32 bytes.");
        }

        ArgumentNullException.ThrowIfNull(payload);

        if (nonce.Length != NonceLength)
        {
            throw new ArgumentOutOfRangeException(nameof(nonce), "The nonce must be exactly 32 bytes.");
        }

        return EncryptCore(key, payload, nonce, footer, implicitAssertion);
    }

    /// <summary>
    /// Decrypts and authenticates a PASETO v4.local token.
    /// </summary>
    /// <param name="key">The 32-byte key.</param>
    /// <param name="token">The PASETO v4.local token.</param>
    /// <param name="expectedFooter">Optional expected footer; verified in constant time when provided.</param>
    /// <param name="implicitAssertion">The implicit assertion that was used during encryption.</param>
    /// <returns>The decrypted payload.</returns>
    /// <exception cref="PasetoException">Any failure: invalid key length, malformed token, strict encoding violation, authentication failure, footer mismatch, or failed decryption.</exception>
    public static string Decrypt(ReadOnlySpan<byte> key, string token, string? expectedFooter = null, string? implicitAssertion = null)
    {
        if (token is null)
        {
            throw new PasetoException("The token must not be null.");
        }

        try
        {
            return DecryptCore(key, token, expectedFooter, implicitAssertion);
        }
        catch (PasetoException)
        {
            throw;
        }
        catch (FormatException ex)
        {
            throw new PasetoException("The token is not valid unpadded base64url.", ex);
        }
        catch (DecoderFallbackException ex)
        {
            throw new PasetoException("The token contains invalid UTF-8 content.", ex);
        }
    }

    /// <summary>
    /// Shared encryption core used by both public entry points.
    /// </summary>
    private static string EncryptCore(ReadOnlySpan<byte> key, string payload, ReadOnlySpan<byte> nonce, string? footer, string? implicitAssertion)
    {
        byte[] payloadBytes = StrictUtf8.GetBytes(payload);
        byte[] footerBytes = StrictUtf8.GetBytes(footer ?? string.Empty);
        byte[] assertion = StrictUtf8.GetBytes(implicitAssertion ?? string.Empty);

        byte[] tmp = KeyedBlake2b.Hash(key, Concatenate(Encoding.ASCII.GetBytes("paseto-encryption-key"), nonce), 56);
        byte[] encryptionKey = tmp.AsSpan(0, 32).ToArray();
        byte[] nonce2 = tmp.AsSpan(32, 24).ToArray();
        byte[] authKey = KeyedBlake2b.Hash(key, Concatenate(Encoding.ASCII.GetBytes("paseto-auth-key-for-aead"), nonce), 32);

        byte[] ciphertext = new byte[payloadBytes.Length];
        XChaCha20Stream.Xor(payloadBytes, encryptionKey, nonce2, ciphertext);

        byte[] preAuth = Pae.Encode(StrictUtf8.GetBytes(Header), nonce.ToArray(), ciphertext, footerBytes, assertion);
        byte[] tag = KeyedBlake2b.Hash(authKey, preAuth, TagLength);

        var combined = new byte[NonceLength + ciphertext.Length + TagLength];
        nonce.CopyTo(combined);
        ciphertext.CopyTo(combined.AsSpan(NonceLength));
        tag.CopyTo(combined.AsSpan(NonceLength + ciphertext.Length));

        string token = Header + Base64Url.Encode(combined);
        if (footerBytes.Length > 0)
        {
            token += "." + Base64Url.Encode(footerBytes);
        }

        return token;
    }

    /// <summary>
    /// Shared decryption core used by the public entry point.
    /// </summary>
    private static string DecryptCore(ReadOnlySpan<byte> key, string token, string? expectedFooter, string? implicitAssertion)
    {
        if (key.Length != KeyLength)
        {
            throw new PasetoException("PASETO v4.local keys must be exactly 32 bytes.");
        }

        if (!token.StartsWith(Header, StringComparison.Ordinal))
        {
            throw new PasetoException("The token must begin with \"v4.local.\".");
        }

        if (token.Length > MaxTokenLength)
        {
            throw new PasetoException("The token exceeds the maximum supported length.");
        }

        ReadOnlySpan<char> remainder = token.AsSpan(Header.Length);
        int separator = remainder.LastIndexOf('.');
        string body = separator >= 0 ? remainder[..separator].ToString() : remainder.ToString();
        string? footerEncoded = separator >= 0 ? remainder[(separator + 1)..].ToString() : null;

        byte[] bodyBytes = Base64Url.Decode(body);
        if (bodyBytes.Length < NonceLength + TagLength)
        {
            throw new PasetoException("The token payload is truncated.");
        }

        byte[] nonce = bodyBytes.AsSpan(0, NonceLength).ToArray();
        byte[] tag = bodyBytes.AsSpan(bodyBytes.Length - TagLength, TagLength).ToArray();
        byte[] ciphertext = bodyBytes.AsSpan(NonceLength, bodyBytes.Length - NonceLength - TagLength).ToArray();

        string footer = footerEncoded is null ? string.Empty : StrictUtf8.GetString(Base64Url.Decode(footerEncoded));
        if (expectedFooter is not null)
        {
            byte[] expected = StrictUtf8.GetBytes(expectedFooter);
            byte[] actual = StrictUtf8.GetBytes(footer);
            if (!CryptographicOperations.FixedTimeEquals(expected, actual))
            {
                throw new PasetoException("The token footer does not match the expected footer.");
            }
        }

        byte[] tmp = KeyedBlake2b.Hash(key, Concatenate(Encoding.ASCII.GetBytes("paseto-encryption-key"), nonce), 56);
        byte[] encryptionKey = tmp.AsSpan(0, 32).ToArray();
        byte[] nonce2 = tmp.AsSpan(32, 24).ToArray();
        byte[] authKey = KeyedBlake2b.Hash(key, Concatenate(Encoding.ASCII.GetBytes("paseto-auth-key-for-aead"), nonce), 32);

        byte[] assertion = StrictUtf8.GetBytes(implicitAssertion ?? string.Empty);
        byte[] preAuth = Pae.Encode(StrictUtf8.GetBytes(Header), nonce, ciphertext, StrictUtf8.GetBytes(footer), assertion);
        byte[] expectedTag = KeyedBlake2b.Hash(authKey, preAuth, TagLength);

        if (!CryptographicOperations.FixedTimeEquals(tag, expectedTag))
        {
            throw new PasetoException("The authentication tag is invalid; the token has been tampered with or the key is wrong.");
        }

        byte[] plaintext = new byte[ciphertext.Length];
        XChaCha20Stream.Xor(ciphertext, encryptionKey, nonce2, plaintext);

        return StrictUtf8.GetString(plaintext);
    }

    /// <summary>
    /// Concatenates two byte buffers.
    /// </summary>
    private static byte[] Concatenate(ReadOnlySpan<byte> first, ReadOnlySpan<byte> second)
    {
        var result = new byte[first.Length + second.Length];
        first.CopyTo(result);
        second.CopyTo(result.AsSpan(first.Length));
        return result;
    }
}
