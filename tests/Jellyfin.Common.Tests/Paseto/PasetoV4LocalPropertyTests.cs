using System;
using System.Text;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Jellyfin.Paseto;
using Xunit;

namespace Jellyfin.Common.Tests.Paseto;

/// <summary>
/// TC-110: FsCheck property tests for the PASETO v4.local implementation.
/// These complement the byte-exact official vectors: the vectors prove conformance
/// for fixed inputs, the properties prove the invariants hold for arbitrary inputs.
/// </summary>
public class PasetoV4LocalPropertyTests
{
    private static readonly byte[] _key = CreateKey(0);
    private static readonly byte[] _otherKey = CreateKey(1);

    [Property]
    public Property RoundTrip_AnyPayload(string? payload)
    {
        string message = payload ?? string.Empty;
        string token = PasetoV4Local.Encrypt(_key, message);

        return (PasetoV4Local.Decrypt(_key, token) == message).ToProperty();
    }

    [Property]
    public Property RoundTrip_WithAnyFooter(string? payload, string? footer)
    {
        string message = payload ?? string.Empty;
        string footerText = footer ?? string.Empty;
        string token = PasetoV4Local.Encrypt(_key, message, footerText);

        return (PasetoV4Local.Decrypt(_key, token, footerText) == message).ToProperty();
    }

    [Property]
    public Property RoundTrip_WithAnyImplicitAssertion(string? payload, string? assertion)
    {
        string message = payload ?? string.Empty;
        string assertionText = assertion ?? string.Empty;
        string token = PasetoV4Local.Encrypt(_key, message, implicitAssertion: assertionText);

        return (PasetoV4Local.Decrypt(_key, token, implicitAssertion: assertionText) == message).ToProperty();
    }

    [Property]
    public Property Encrypt_UsesFreshNonce_SoTokensDiffer(string? payload)
    {
        string message = payload ?? string.Empty;
        string first = PasetoV4Local.Encrypt(_key, message);
        string second = PasetoV4Local.Encrypt(_key, message);

        return (first != second
            && PasetoV4Local.Decrypt(_key, first) == message
            && PasetoV4Local.Decrypt(_key, second) == message).ToProperty();
    }

    [Property]
    public Property Decrypt_WithWrongKey_Throws(string? payload)
    {
        string token = PasetoV4Local.Encrypt(_key, payload ?? string.Empty);

        return Throws<PasetoException>(() => PasetoV4Local.Decrypt(_otherKey, token)).ToProperty();
    }

    [Property]
    public Property Decrypt_WithWrongFooter_Throws(string? payload, string? footer, string? otherFooter)
    {
        string footerText = footer ?? string.Empty;
        string otherFooterText = otherFooter ?? string.Empty;
        string token = PasetoV4Local.Encrypt(_key, payload ?? string.Empty, footerText);

        return (footerText == otherFooterText
            || Throws<PasetoException>(() => PasetoV4Local.Decrypt(_key, token, otherFooterText))).ToProperty();
    }

    [Property]
    public Property Base64Url_RoundTrips(byte[]? data)
    {
        byte[] bytes = data ?? Array.Empty<byte>();

        return Base64Url.Decode(Base64Url.Encode(bytes)).AsSpan().SequenceEqual(bytes).ToProperty();
    }

    [Property]
    public Property Pae_SinglePiece_IsInjective(string? first, string? second)
    {
        string a = first ?? string.Empty;
        string b = second ?? string.Empty;
        byte[] encodedA = Pae.Encode(Encoding.UTF8.GetBytes(a));
        byte[] encodedB = Pae.Encode(Encoding.UTF8.GetBytes(b));

        return (a == b || !encodedA.AsSpan().SequenceEqual(encodedB)).ToProperty();
    }

    [Property]
    public Property Pae_FramingIsPrefixFree(string? first, string? second)
    {
        string a = first ?? string.Empty;
        string b = second ?? string.Empty;

        // LE64 length framing means two pieces never encode the same as their concatenation.
        byte[] concatenated = Pae.Encode(Encoding.UTF8.GetBytes(a + b));
        byte[] twoPieces = Pae.Encode(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

        return (!concatenated.AsSpan().SequenceEqual(twoPieces)).ToProperty();
    }

    private static byte[] CreateKey(byte seed)
    {
        byte[] key = new byte[32];
        for (int i = 0; i < key.Length; i++)
        {
            key[i] = (byte)(i + seed);
        }

        return key;
    }

    private static bool Throws<TException>(Action action)
        where TException : Exception
    {
        try
        {
            action();
            return false;
        }
        catch (TException)
        {
            return true;
        }
    }
}
