using System;

namespace MediaBrowser.Model.Plugins;

/// <summary>
/// Exception for plugin configuration validation.
/// </summary>
public class ConfigurationValidationException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
    /// </summary>
    public ConfigurationValidationException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConfigurationValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationValidationException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ConfigurationValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
