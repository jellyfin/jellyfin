using System;

namespace MediaBrowser.Common;

/// <summary>
/// Represents errors that occur during interaction with FFmpeg.
/// </summary>
public class FfmpegException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegException"/> class.
    /// </summary>
    public FfmpegException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public FfmpegException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FfmpegException"/> class with a specified error message and a
    /// reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">
    /// The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if
    /// no inner exception is specified.
    /// </param>
    public FfmpegException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
