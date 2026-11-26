using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using MediaBrowser.Common.Telemetry;
using Xunit;

namespace Jellyfin.Controller.Tests.Telemetry;

/// <summary>
/// Collects the measurements the Jellyfin meter publishes while a piece of code runs.
/// </summary>
internal static class MeterCollector
{
    /// <summary>
    /// Runs <paramref name="act"/> and returns everything the Jellyfin meter published while it ran,
    /// including a reading of every observable instrument taken afterwards.
    /// </summary>
    /// <param name="act">The code to run.</param>
    /// <returns>The measurements that were published.</returns>
    internal static List<Measured> Collect(Action act)
    {
        var measurements = new List<Measured>();

        using var listener = new MeterListener();
        listener.InstrumentPublished = (instrument, l) =>
        {
            if (ReferenceEquals(instrument.Meter, JellyfinTelemetry.Meter))
            {
                l.EnableMeasurementEvents(instrument);
            }
        };

        listener.SetMeasurementEventCallback<int>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.SetMeasurementEventCallback<long>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.SetMeasurementEventCallback<double>((instrument, value, tags, _) => Record(measurements, instrument, value, tags));
        listener.Start();

        act();
        listener.RecordObservableInstruments();

        return measurements;
    }

    /// <summary>
    /// Returns the value of the single measurement of <paramref name="instrument"/> carrying
    /// <paramref name="tags"/>, and fails when there is not exactly one.
    /// </summary>
    /// <param name="measurements">The collected measurements.</param>
    /// <param name="instrument">The name of the instrument.</param>
    /// <param name="tags">The tags the measurement must carry.</param>
    /// <returns>The value that was recorded.</returns>
    internal static double Value(List<Measured> measurements, string instrument, params (string Name, string Value)[] tags)
        => Assert.Single(measurements, m => m.Matches(instrument, tags)).Value;

    private static void Record(List<Measured> measurements, Instrument instrument, double value, ReadOnlySpan<KeyValuePair<string, object?>> tags)
    {
        var recorded = new Dictionary<string, string?>(tags.Length, StringComparer.Ordinal);
        foreach (var tag in tags)
        {
            recorded[tag.Key] = tag.Value?.ToString();
        }

        measurements.Add(new Measured(instrument.Name, value, recorded));
    }
}
