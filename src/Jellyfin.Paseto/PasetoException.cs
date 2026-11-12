using System;

namespace Jellyfin.Paseto;

/// <summary>
/// Thrown when a PASETO v4.local operation fails, including key length violations,
/// malformed tokens, strict encoding violations, authentication failures and
/// decryption failures.
/// </summary>
public sealed class PasetoException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PasetoException"/> class.
    /// </summary>
    public PasetoException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasetoException"/> class with a message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public PasetoException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PasetoException"/> class with a message and an inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of this exception.</param>
    public PasetoException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
