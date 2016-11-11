namespace SocketHttpListener
{
  /// <summary>
  /// Contains the values of the compression method used to compress the message on the WebSocket
  /// connection.
  /// </summary>
  /// <remarks>
  /// The values of the compression method are defined in
  /// <see href="http://tools.ietf.org/html/draft-ietf-hybi-permessage-compression-09">Compression
  /// Extensions for WebSocket</see>.
  /// </remarks>
  public enum CompressionMethod : byte
  {
    /// <summary>
    /// Indicates non compression.
    /// </summary>
    None,
    /// <summary>
    /// Indicates using DEFLATE.
    /// </summary>
    Deflate
  }
}
