using System;

namespace SocketHttpListener
{
  /// <summary>
  /// The exception that is thrown when a <see cref="WebSocket"/> gets a fatal error.
  /// </summary>
  public class WebSocketException : Exception
  {
    #region Internal Constructors

    internal WebSocketException ()
      : this (CloseStatusCode.Abnormal, null, null)
    {
    }

    internal WebSocketException (string message)
      : this (CloseStatusCode.Abnormal, message, null)
    {
    }

    internal WebSocketException (CloseStatusCode code)
      : this (code, null, null)
    {
    }

    internal WebSocketException (string message, Exception innerException)
      : this (CloseStatusCode.Abnormal, message, innerException)
    {
    }

    internal WebSocketException (CloseStatusCode code, string message)
      : this (code, message, null)
    {
    }

    internal WebSocketException (CloseStatusCode code, string message, Exception innerException)
      : base (message ?? code.GetMessage (), innerException)
    {
      Code = code;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the status code indicating the cause for the exception.
    /// </summary>
    /// <value>
    /// One of the <see cref="CloseStatusCode"/> enum values, represents the status code indicating
    /// the cause for the exception.
    /// </value>
    public CloseStatusCode Code {
      get; private set;
    }

    #endregion
  }
}
