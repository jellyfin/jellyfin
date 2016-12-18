using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SocketHttpListener
{
  internal class PayloadData : IEnumerable<byte>
  {
    #region Private Fields

    private byte [] _applicationData;
    private byte [] _extensionData;
    private bool    _masked;

    #endregion

    #region Public Const Fields

    public const ulong MaxLength = long.MaxValue;

    #endregion

    #region Public Constructors

    public PayloadData ()
      : this (new byte [0], new byte [0], false)
    {
    }

    public PayloadData (byte [] applicationData)
      : this (new byte [0], applicationData, false)
    {
    }

    public PayloadData (string applicationData)
      : this (new byte [0], Encoding.UTF8.GetBytes (applicationData), false)
    {
    }

    public PayloadData (byte [] applicationData, bool masked)
      : this (new byte [0], applicationData, masked)
    {
    }

    public PayloadData (byte [] extensionData, byte [] applicationData, bool masked)
    {
      _extensionData = extensionData;
      _applicationData = applicationData;
      _masked = masked;
    }

    #endregion

    #region Internal Properties

    internal bool ContainsReservedCloseStatusCode {
      get {
        return _applicationData.Length > 1 &&
               _applicationData.SubArray (0, 2).ToUInt16 (ByteOrder.Big).IsReserved ();
      }
    }

    #endregion

    #region Public Properties

    public byte [] ApplicationData {
      get {
        return _applicationData;
      }
    }

    public byte [] ExtensionData {
      get {
        return _extensionData;
      }
    }

    public bool IsMasked {
      get {
        return _masked;
      }
    }

    public ulong Length {
      get {
        return (ulong) (_extensionData.Length + _applicationData.Length);
      }
    }

    #endregion

    #region Private Methods

    private static void mask (byte [] src, byte [] key)
    {
      for (long i = 0; i < src.Length; i++)
        src [i] = (byte) (src [i] ^ key [i % 4]);
    }

    #endregion

    #region Public Methods

    public IEnumerator<byte> GetEnumerator ()
    {
      foreach (byte b in _extensionData)
        yield return b;

      foreach (byte b in _applicationData)
        yield return b;
    }

    public void Mask (byte [] maskingKey)
    {
      if (_extensionData.Length > 0)
        mask (_extensionData, maskingKey);

      if (_applicationData.Length > 0)
        mask (_applicationData, maskingKey);

      _masked = !_masked;
    }

    public byte [] ToByteArray ()
    {
      return _extensionData.Length > 0
             ? new List<byte> (this).ToArray ()
             : _applicationData;
    }

    public override string ToString ()
    {
      return BitConverter.ToString (ToByteArray ());
    }

    #endregion

    #region Explicitly Implemented Interface Members

    IEnumerator IEnumerable.GetEnumerator ()
    {
      return GetEnumerator ();
    }

    #endregion
  }
}
