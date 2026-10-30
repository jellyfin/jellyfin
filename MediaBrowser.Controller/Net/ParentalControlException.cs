namespace MediaBrowser.Controller.Net;

/// <summary>
/// The exception that is thrown when parental controls prevent a user from accessing the server.
/// </summary>
public sealed class ParentalControlException : SecurityException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ParentalControlException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ParentalControlException(string message)
        : base(message)
    {
    }
}
