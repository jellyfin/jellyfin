using System;
using System.Text;

namespace SocketHttpListener
{
  /// <summary>
  /// Contains the event data associated with a <see cref="WebSocket.OnClose"/> event.
  /// </summary>
  /// <remarks>
  /// A <see cref="WebSocket.OnClose"/> event occurs when the WebSocket connection has been closed.
  /// If you would like to get the reason for the close, you should access the <see cref="Code"/> or
  /// <see cref="Reason"/> property.
  /// </remarks>
  public class CloseEventArgs : EventArgs
  {
    #region Private Fields

    private bool   _clean;
    private ushort _code;
    private string _reason;

    #endregion

    #region Internal Constructors

    internal CloseEventArgs (PayloadData payload)
    {
      var data = payload.ApplicationData;
      var len = data.Length;
      _code = len > 1
              ? data.SubArray (0, 2).ToUInt16 (ByteOrder.Big)
              : (ushort) CloseStatusCode.NoStatusCode;

      _reason = len > 2
                ? GetUtf8String(data.SubArray (2, len - 2))
                : String.Empty;
    }

      private string GetUtf8String(byte[] bytes)
      {
          return Encoding.UTF8.GetString(bytes, 0, bytes.Length);
      }

    #endregion

    #region Public Properties

    /// <summary>
    /// Gets the status code for the close.
    /// </summary>
    /// <value>
    /// A <see cref="ushort"/> that represents the status code for the close if any.
    /// </value>
    public ushort Code {
      get {
        return _code;
      }
    }

    /// <summary>
    /// Gets the reason for the close.
    /// </summary>
    /// <value>
    /// A <see cref="string"/> that represents the reason for the close if any.
    /// </value>
    public string Reason {
      get {
        return _reason;
      }
    }

    /// <summary>
    /// Gets a value indicating whether the WebSocket connection has been closed cleanly.
    /// </summary>
    /// <value>
    /// <c>true</c> if the WebSocket connection has been closed cleanly; otherwise, <c>false</c>.
    /// </value>
    public bool WasClean {
      get {
        return _clean;
      }

      internal set {
        _clean = value;
      }
    }

    #endregion
  }
}
