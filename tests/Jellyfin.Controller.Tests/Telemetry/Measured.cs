using System;
using System.Collections.Generic;

namespace Jellyfin.Controller.Tests.Telemetry;

/// <summary>
/// A single measurement published on the Jellyfin meter.
/// </summary>
/// <param name="Instrument">The name of the instrument.</param>
/// <param name="Value">The value that was recorded.</param>
/// <param name="Tags">The tags the measurement carries.</param>
internal sealed record Measured(string Instrument, double Value, IReadOnlyDictionary<string, string?> Tags)
{
    internal bool Matches(string instrument, params (string Name, string Value)[] tags)
    {
        if (!string.Equals(Instrument, instrument, StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var (name, value) in tags)
        {
            if (!Tags.TryGetValue(name, out var recorded) || !string.Equals(recorded, value, StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
