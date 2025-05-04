using System;
using System.Globalization;
using System.Text;

namespace Jellyfin.Api.Helpers;

/// <summary>
/// Helpers to generate HLS codec strings according to
/// <a href="https://datatracker.ietf.org/doc/html/rfc6381#section-3.3">RFC 6381 section 3.3</a>
/// and the <a href="https://mp4ra.org">MP4 Registration Authority</a>.
/// </summary>
public static class HlsCodecStringHelpers
{
    /// <summary>
    /// Codec name for MP3.
    /// </summary>
    public const string MP3 = "mp4a.40.34";

    /// <summary>
    /// Codec name for AC-3.
    /// </summary>
    public const string AC3 = "ac-3";

    /// <summary>
    /// Codec name for E-AC-3.
    /// </summary>
    public const string EAC3 = "ec-3";

    /// <summary>
    /// Codec name for FLAC.
    /// </summary>
    public const string FLAC = "fLaC";

    /// <summary>
    /// Codec name for ALAC.
    /// </summary>
    public const string ALAC = "alac";

    /// <summary>
    /// Codec name for OPUS.
    /// </summary>
    public const string OPUS = "Opus";

    /// <summary>
    /// Gets a MP3 codec string.
    /// </summary>
    /// <returns>MP3 codec string.</returns>
    public static string GetMP3String()
    {
        return MP3;
    }

    /// <summary>
    /// Gets an AAC codec string.
    /// </summary>
    /// <param name="profile">AAC profile.</param>
    /// <returns>AAC codec string.</returns>
    public static string GetAACString(string? profile)
    {
        StringBuilder result = new StringBuilder("mp4a", 9);

        if (string.Equals(profile, "HE", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".40.5");
        }
        else
        {
            // Default to LC if profile is invalid
            result.Append(".40.2");
        }

        return result.ToString();
    }

    /// <summary>
    /// Gets an AC-3 codec string.
    /// </summary>
    /// <returns>AC-3 codec string.</returns>
    public static string GetAC3String()
    {
        return AC3;
    }

    /// <summary>
    /// Gets an E-AC-3 codec string.
    /// </summary>
    /// <returns>E-AC-3 codec string.</returns>
    public static string GetEAC3String()
    {
        return EAC3;
    }

    /// <summary>
    /// Gets an FLAC codec string.
    /// </summary>
    /// <returns>FLAC codec string.</returns>
    public static string GetFLACString()
    {
        return FLAC;
    }

    /// <summary>
    /// Gets an ALAC codec string.
    /// </summary>
    /// <returns>ALAC codec string.</returns>
    public static string GetALACString()
    {
        return ALAC;
    }

    /// <summary>
    /// Gets an OPUS codec string.
    /// </summary>
    /// <returns>OPUS codec string.</returns>
    public static string GetOPUSString()
    {
        return OPUS;
    }

    /// <summary>
    /// Gets a H.264 codec string.
    /// </summary>
    /// <param name="profile">H.264 profile.</param>
    /// <param name="level">H.264 level.</param>
    /// <returns>H.264 string.</returns>
    public static string GetH264String(string? profile, int level)
    {
        StringBuilder result = new StringBuilder("avc1", 11);

        if (string.Equals(profile, "high", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".6400");
        }
        else if (string.Equals(profile, "main", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".4D40");
        }
        else if (string.Equals(profile, "baseline", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".42E0");
        }
        else
        {
            // Default to constrained baseline if profile is invalid
            result.Append(".4240");
        }

        string levelHex = level.ToString("X2", CultureInfo.InvariantCulture);
        result.Append(levelHex);

        return result.ToString();
    }

    /// <summary>
    /// Gets a H.265 codec string.
    /// </summary>
    /// <param name="profile">H.265 profile.</param>
    /// <param name="level">H.265 level.</param>
    /// <returns>H.265 string.</returns>
    public static string GetH265String(string? profile, int level)
    {
        // The h265 syntax is a bit of a mystery at the time this comment was written.
        // This is what I've found through various sources:
        // FORMAT: [codecTag].[profile].[constraint?].L[level * 30].[UNKNOWN]
        StringBuilder result = new StringBuilder("hvc1", 16);

        if (string.Equals(profile, "main10", StringComparison.OrdinalIgnoreCase)
            || string.Equals(profile, "main 10", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".2.4");
        }
        else
        {
            // Default to main if profile is invalid
            result.Append(".1.4");
        }

        result.Append(".L")
            .Append(level)
            .Append(".B0");

        return result.ToString();
    }

    /// <summary>
    /// Gets a VP9 codec string.
    /// </summary>
    /// <param name="width">Video width.</param>
    /// <param name="height">Video height.</param>
    /// <param name="pixelFormat">Video pixel format.</param>
    /// <param name="framerate">Video framerate.</param>
    /// <param name="bitDepth">Video bitDepth.</param>
    /// <returns>The VP9 codec string.</returns>
    public static string GetVp9String(int width, int height, string pixelFormat, float framerate, int bitDepth)
    {
        // refer: https://www.webmproject.org/vp9/mp4/
        StringBuilder result = new StringBuilder("vp09", 13);

        var profileString = pixelFormat switch
        {
            "yuv420p" => "00",
            "yuvj420p" => "00",
            "yuv422p" => "01",
            "yuv444p" => "01",
            "yuv420p10le" => "02",
            "yuv420p12le" => "02",
            "yuv422p10le" => "03",
            "yuv422p12le" => "03",
            "yuv444p10le" => "03",
            "yuv444p12le" => "03",
            _ => "00"
        };

        var lumaPictureSize = width * height;
        var lumaSampleRate = lumaPictureSize * framerate;
        var levelString = lumaPictureSize switch
        {
            <= 0 => "00",
            <= 36864 => "10",
            <= 73728 => "11",
            <= 122880 => "20",
            <= 245760 => "21",
            <= 552960 => "30",
            <= 983040 => "31",
            <= 2228224 => lumaSampleRate <= 83558400 ? "40" : "41",
            <= 8912896 => lumaSampleRate <= 311951360 ? "50" : (lumaSampleRate <= 588251136 ? "51" : "52"),
            <= 35651584 => lumaSampleRate <= 1176502272 ? "60" : (lumaSampleRate <= 4706009088 ? "61" : "62"),
            _ => "00" // This should not happen
        };

        if (bitDepth != 8
            && bitDepth != 10
            && bitDepth != 12)
        {
            // Default to 8 bits
            bitDepth = 8;
        }

        result.Append('.').Append(profileString).Append('.').Append(levelString);
        var bitDepthD2 = bitDepth.ToString("D2", CultureInfo.InvariantCulture);
        result.Append('.')
            .Append(bitDepthD2);

        return result.ToString();
    }

    /// <summary>
    /// Gets an AV1 codec string.
    /// </summary>
    /// <param name="profile">AV1 profile.</param>
    /// <param name="level">AV1 level.</param>
    /// <param name="tierFlag">AV1 tier flag.</param>
    /// <param name="bitDepth">AV1 bit depth.</param>
    /// <returns>The AV1 codec string.</returns>
    public static string GetAv1String(string? profile, int level, bool tierFlag, int bitDepth)
    {
        // https://aomediacodec.github.io/av1-isobmff/#codecsparam
        // FORMAT: [codecTag].[profile].[level][tier].[bitDepth]
        StringBuilder result = new StringBuilder("av01", 13);

        if (string.Equals(profile, "Main", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".0");
        }
        else if (string.Equals(profile, "High", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".1");
        }
        else if (string.Equals(profile, "Professional", StringComparison.OrdinalIgnoreCase))
        {
            result.Append(".2");
        }
        else
        {
            // Default to Main
            result.Append(".0");
        }

        if (level is <= 0 or > 31)
        {
            // Default to the maximum defined level 6.3
            level = 19;
        }

        if (bitDepth != 8
            && bitDepth != 10
            && bitDepth != 12)
        {
            // Default to 8 bits
            bitDepth = 8;
        }

        result.Append('.')
            // Needed to pad it double digits; otherwise, browsers will reject the stream.
            .AppendFormat(CultureInfo.InvariantCulture, "{0:D2}", level)
            .Append(tierFlag ? 'H' : 'M');

        string bitDepthD2 = bitDepth.ToString("D2", CultureInfo.InvariantCulture);
        result.Append('.')
            .Append(bitDepthD2);

        return result.ToString();
    }
}
