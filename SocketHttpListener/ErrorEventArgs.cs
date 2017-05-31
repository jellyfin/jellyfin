using System;

namespace SocketHttpListener
{
  /// <summary>
  /// Contains the event data associated with a <see cref="WebSocket.OnError"/> event.
  /// </summary>
  /// <remarks>
  /// A <see cref="WebSocket.OnError"/> event occurs when the <see cref="WebSocket"/> gets an error.
  /// If you would like to get the error message, you should access the <see cref="Message"/>
  /// property.
  /// </remarks>
  public class ErrorEventArgs : EventArgs
  {
    #region Private Fields

    private string _message;

    #endregion

    #region Internal Constructors

    internal ErrorEventArgs (string message)
    {
      _message = message;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the error message.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the error message.
    /// </value>
    public string Message {
      get {
        return _message;
      }
    }

    #endregion
  }
}
