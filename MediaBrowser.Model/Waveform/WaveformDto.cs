using System.Collections.Generic;

namespace MediaBrowser.Model.Waveform;

/// <summary>
/// Waveform response model.
/// </summary>
public class WaveformDto
{
    /// <summary>
    /// Gets or sets the duration of each sample in seconds.
    /// </summary>
    public double SampleDuration { get; set; }

    /// <summary>
    /// Gets or sets the waveform amplitude samples in linear scale (0.0 to 1.0).
    /// </summary>
    public IReadOnlyList<double> Samples { get; set; } = [];
}
