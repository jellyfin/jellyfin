namespace SocketHttpListener
{
  /// <summary>
  /// Contains the values of the opcode that indicates the type of a WebSocket frame.
  /// </summary>
  /// <remarks>
  /// The values of the opcode are defined in
  /// <see href="http://tools.ietf.org/html/rfc6455#section-5.2">Section 5.2</see> of RFC 6455.
  /// </remarks>
  public enum Opcode : byte
  {
    /// <summary>
    /// Equivalent to numeric value 0.
    /// Indicates a continuation frame.
    /// </summary>
    Cont = 0x0,
    /// <summary>
    /// Equivalent to numeric value 1.
    /// Indicates a text frame.
    /// </summary>
    Text = 0x1,
    /// <summary>
    /// Equivalent to numeric value 2.
    /// Indicates a binary frame.
    /// </summary>
    Binary = 0x2,
    /// <summary>
    /// Equivalent to numeric value 8.
    /// Indicates a connection close frame.
    /// </summary>
    Close = 0x8,
    /// <summary>
    /// Equivalent to numeric value 9.
    /// Indicates a ping frame.
    /// </summary>
    Ping = 0x9,
    /// <summary>
    /// Equivalent to numeric value 10.
    /// Indicates a pong frame.
    /// </summary>
    Pong = 0xa
  }
}
