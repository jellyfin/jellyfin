using System.Collections.Generic;

namespace MediaBrowser.Model.MediaInfo;

/// <summary>
/// A clip of the main playlist of a Blu-ray ISO image.
/// </summary>
/// <param name="DurationSeconds">The playback duration of the clip in seconds.</param>
/// <param name="Extents">The byte ranges of the clip inside the image, end exclusive.</param>
public record BlurayIsoClip(double DurationSeconds, IReadOnlyList<(long Start, long End)> Extents);
