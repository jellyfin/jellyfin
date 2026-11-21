using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Server.Telemetry;

/// <summary>
/// Surfaces OpenTelemetry SDK problems, most importantly failed exports, in the server log.
/// </summary>
internal sealed class OpenTelemetryEventListener : EventListener, IHostedService
{
    private const string EventSourceNamePrefix = "OpenTelemetry";

    /// <summary>
    /// Events that the SDK raises at warning level but that are the expected outcome of the
    /// configuration, not a problem to act on.
    /// </summary>
    private static readonly HashSet<string> _benignEventNames = new(StringComparer.Ordinal)
    {
        // Raised once per instrument published by a Meter the provider does not subscribe to,
        // e.g. the msquic and other framework-internal meters. Not subscribing to them is the
        // point of the meter allowlist, and the event's own text says it can be ignored.
        "MetricInstrumentIgnored"
    };

    /// <summary>
    /// Reporting is throttled because an export failure can itself be logged, exported and fail again.
    /// </summary>
    private static readonly TimeSpan _reportInterval = TimeSpan.FromMinutes(5);

    private readonly ILogger<OpenTelemetryEventListener>? _logger;

    private long _lastReportTimestamp;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenTelemetryEventListener"/> class.
    /// </summary>
    /// <param name="logger">Instance of the <see cref="ILogger{TCategoryName}"/> interface.</param>
    public OpenTelemetryEventListener(ILogger<OpenTelemetryEventListener> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        base.OnEventSourceCreated(eventSource);

        if (eventSource.Name.StartsWith(EventSourceNamePrefix, StringComparison.Ordinal))
        {
            EnableEvents(eventSource, EventLevel.Warning);
        }
    }

    /// <inheritdoc />
    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // Sources can be enabled before the base constructor returns, so the logger may not be set yet.
        var logger = _logger;
        if (logger is null
            || eventData.EventSource?.Name.StartsWith(EventSourceNamePrefix, StringComparison.Ordinal) != true)
        {
            return;
        }

        // Checked before ShouldReport so that benign chatter does not spend the report budget
        // and hide a real export failure for the rest of the interval.
        if (_benignEventNames.Contains(eventData.EventName ?? string.Empty))
        {
            logger.LogDebug(
                "OpenTelemetry reported {EventName}: {Message}",
                eventData.EventName,
                FormatMessage(eventData));
            return;
        }

        if (!ShouldReport())
        {
            return;
        }

        logger.LogWarning(
            "OpenTelemetry reported {EventName}: {Message}",
            eventData.EventName,
            FormatMessage(eventData));
    }

    private static string FormatMessage(EventWrittenEventArgs eventData)
    {
        var payload = eventData.Payload?.ToArray() ?? [];
        if (string.IsNullOrEmpty(eventData.Message))
        {
            return string.Join(", ", payload);
        }

        try
        {
            return string.Format(CultureInfo.InvariantCulture, eventData.Message, payload);
        }
        catch (FormatException)
        {
            return eventData.Message;
        }
    }

    private bool ShouldReport()
    {
        var now = Stopwatch.GetTimestamp();
        var last = Interlocked.Read(ref _lastReportTimestamp);
        if (last != 0 && Stopwatch.GetElapsedTime(last, now) < _reportInterval)
        {
            return false;
        }

        return Interlocked.CompareExchange(ref _lastReportTimestamp, now, last) == last;
    }
}
