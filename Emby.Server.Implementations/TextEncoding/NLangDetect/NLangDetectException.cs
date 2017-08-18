using System;

namespace NLangDetect.Core
{
  public class NLangDetectException : Exception
  {
    #region Constructor(s)

    public NLangDetectException(string message, ErrorCode errorCode)
      : base(message)
    {
      ErrorCode = errorCode;
    }

    #endregion

    #region Properties

    public ErrorCode ErrorCode { get; private set; }

    #endregion
  }
}
