using System;
using System.Threading;

namespace Jellyfin.Api.Models.ClipDtos;

/// <summary>
/// Tracks a clip extraction job in progress.
/// </summary>
internal sealed class ClipJob
{
    private double _progressPercent;
    private bool _isComplete;
    private bool _hasError;
    private string? _errorMessage;

    /// <summary>Gets the unique clip job identifier.</summary>
    public required string ClipId { get; init; }

    /// <summary>Gets the media item id this clip belongs to.</summary>
    public required Guid ItemId { get; init; }

    /// <summary>Gets the output file path on disk.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Gets the media item name (used for the download filename).</summary>
    public string? ItemName { get; init; }

    /// <summary>Gets the clip start position in ticks.</summary>
    public long StartTimeTicks { get; init; }

    /// <summary>Gets the clip end position in ticks.</summary>
    public long EndTimeTicks { get; init; }

    /// <summary>Gets the clip duration in ticks.</summary>
    public long DurationTicks { get; init; }

    /// <summary>Gets the cancellation token source used to cancel FFmpeg.</summary>
    public required CancellationTokenSource CancellationTokenSource { get; init; }

    /// <summary>Gets or sets the encoding progress (0–100).</summary>
    public double ProgressPercent
    {
        get => Volatile.Read(ref _progressPercent);
        set => Volatile.Write(ref _progressPercent, value);
    }

    /// <summary>Gets or sets a value indicating whether encoding has completed successfully.</summary>
    public bool IsComplete
    {
        get => Volatile.Read(ref _isComplete);
        set => Volatile.Write(ref _isComplete, value);
    }

    /// <summary>Gets or sets a value indicating whether encoding has failed.</summary>
    public bool HasError
    {
        get => Volatile.Read(ref _hasError);
        set => Volatile.Write(ref _hasError, value);
    }

    /// <summary>Gets or sets the error message if <see cref="HasError"/> is true.</summary>
    public string? ErrorMessage
    {
        get => Volatile.Read(ref _errorMessage);
        set => Volatile.Write(ref _errorMessage, value);
    }
}
