using System.Collections.Generic;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.MediaEncoding;

/// <summary>
/// Describes the downmix algorithms capabilities.
/// </summary>
public static class DownMixAlgorithmsHelper
{
    /// <summary>
    /// The filter string of the DownMixStereoAlgorithms.
    /// The index is the tuple of (algorithm, layout).
    /// </summary>
    public static readonly Dictionary<(DownMixStereoAlgorithms, string), string> AlgorithmFilterStrings = new()
    {
        { (DownMixStereoAlgorithms.Dave750, "5.1"), "pan=stereo|c0=0.5*c2+0.707*c0+0.707*c4+0.5*c3|c1=0.5*c2+0.707*c1+0.707*c5+0.5*c3" },
        // Use AC-4 algorithm to downmix 7.1 inputs to 5.1 first
        { (DownMixStereoAlgorithms.Dave750, "7.1"), "pan=5.1(side)|c0=c0|c1=c1|c2=c2|c3=c3|c4=0.707*c4+0.707*c6|c5=0.707*c5+0.707*c7,pan=stereo|c0=0.5*c2+0.707*c0+0.707*c4+0.5*c3|c1=0.5*c2+0.707*c1+0.707*c5+0.5*c3" },
        { (DownMixStereoAlgorithms.NightmodeDialogue, "5.1"), "pan=stereo|c0=c2+0.30*c0+0.30*c4|c1=c2+0.30*c1+0.30*c5" },
        // Use AC-4 algorithm to downmix 7.1 inputs to 5.1 first
        { (DownMixStereoAlgorithms.NightmodeDialogue, "7.1"), "pan=5.1(side)|c0=c0|c1=c1|c2=c2|c3=c3|c4=0.707*c4+0.707*c6|c5=0.707*c5+0.707*c7,pan=stereo|c0=c2+0.30*c0+0.30*c4|c1=c2+0.30*c1+0.30*c5" },
        { (DownMixStereoAlgorithms.Rfc7845, "3.0"), "pan=stereo|c0=0.414214*c2+0.585786*c0|c1=0.414214*c2+0.585786*c1" },
        { (DownMixStereoAlgorithms.Rfc7845, "quad"), "pan=stereo|c0=0.422650*c0+0.366025*c2+0.211325*c3|c1=0.422650*c1+0.366025*c3+0.211325*c2" },
        { (DownMixStereoAlgorithms.Rfc7845, "5.0"), "pan=stereo|c0=0.460186*c2+0.650802*c0+0.563611*c3+0.325401*c4|c1=0.460186*c2+0.650802*c1+0.563611*c4+0.325401*c3" },
        { (DownMixStereoAlgorithms.Rfc7845, "5.1"), "pan=stereo|c0=0.374107*c2+0.529067*c0+0.458186*c4+0.264534*c5+0.374107*c3|c1=0.374107*c2+0.529067*c1+0.458186*c5+0.264534*c4+0.374107*c3" },
        { (DownMixStereoAlgorithms.Rfc7845, "6.1"), "pan=stereo|c0=0.321953*c2+0.455310*c0+0.394310*c5+0.227655*c6+0.278819*c4+0.321953*c3|c1=0.321953*c2+0.455310*c1+0.394310*c6+0.227655*c5+0.278819*c4+0.321953*c3" },
        { (DownMixStereoAlgorithms.Rfc7845, "7.1"), "pan=stereo|c0=0.274804*c2+0.388631*c0+0.336565*c6+0.194316*c7+0.336565*c4+0.194316*c5+0.274804*c3|c1=0.274804*c2+0.388631*c1+0.336565*c7+0.194316*c6+0.336565*c5+0.194316*c4+0.274804*c3" },
        { (DownMixStereoAlgorithms.Ac4, "3.0"), "pan=stereo|c0=c0+0.707*c2|c1=c1+0.707*c2" },
        { (DownMixStereoAlgorithms.Ac4, "5.0"), "pan=stereo|c0=c0+0.707*c2+0.707*c3|c1=c1+0.707*c2+0.707*c4" },
        { (DownMixStereoAlgorithms.Ac4, "5.1"), "pan=stereo|c0=c0+0.707*c2+0.707*c4|c1=c1+0.707*c2+0.707*c5" },
        { (DownMixStereoAlgorithms.Ac4, "7.0"), "pan=5.0(side)|c0=c0|c1=c1|c2=c2|c3=0.707*c3+0.707*c5|c4=0.707*c4+0.707*c6,pan=stereo|c0=c0+0.707*c2+0.707*c3|c1=c1+0.707*c2+0.707*c4" },
        { (DownMixStereoAlgorithms.Ac4, "7.1"), "pan=5.1(side)|c0=c0|c1=c1|c2=c2|c3=c3|c4=0.707*c4+0.707*c6|c5=0.707*c5+0.707*c7,pan=stereo|c0=c0+0.707*c2+0.707*c4|c1=c1+0.707*c2+0.707*c5" },
    };

    /// <summary>
    /// Get the audio channel layout string from the audio stream
    /// If the input audio string does not have a valid layout string, guess from channel count.
    /// </summary>
    /// <param name="audioStream">The audio stream to get layout.</param>
    /// <returns>Channel Layout string.</returns>
    public static string InferChannelLayout(MediaStream audioStream)
    {
        if (!string.IsNullOrWhiteSpace(audioStream.ChannelLayout))
        {
            // Note: BDMVs do not derive this string from ffmpeg, which would cause ambiguity with 4-channel audio
            // "quad" => 2 front and 2 rear, "4.0" => 3 front and 1 rear
            // BDMV will always use "4.0" in this case
            // Because the quad layout is super rare in BDs, we will use "4.0" as is here
            return audioStream.ChannelLayout;
        }

        if (audioStream.Channels is null)
        {
            return string.Empty;
        }

        // When we don't have definitive channel layout, we have to guess from the channel count
        // Guessing is not always correct, but for most videos we don't have to guess like this as the definitive layout is recorded during scan
        var inferredLayout = audioStream.Channels.Value switch
        {
            1 => "mono",
            2 => "stereo",
            3 => "2.1", // Could also be 3.0, prefer 2.1
            4 => "4.0", // Could also be quad (with rear left and rear right) and 3.1 with LFE. prefer 4.0 with front center and back center
            5 => "5.0",
            6 => "5.1", // Could also be 6.0 or hexagonal, prefer 5.1
            7 => "6.1", // Could also be 7.0, prefer 6.1
            8 => "7.1", // Could also be 8.0, prefer 7.1
            _ => string.Empty // Return empty string for not supported layout
        };
        return inferredLayout;
    }
}
