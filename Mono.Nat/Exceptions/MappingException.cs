//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Security.Permissions;

namespace Mono.Nat
{
    [Serializable]
    public class MappingException : Exception
    {
        public ErrorCode ErrorCode { get; private set; } = ErrorCode.Unknown;
        public string ErrorText { get; private set; }

        #region Constructors
        public MappingException ()
        {
        }

        public MappingException (string message)
            : base (message)
        {
        }

        public MappingException (ErrorCode errorCode, string errorText)
            : base (string.Format ("Error {0}: {1}", errorCode, errorText))
        {
            ErrorCode = errorCode;
            ErrorText = errorText;
        }

        public MappingException (string message, Exception innerException)
            : base (message, innerException)
        {
        }

        protected MappingException (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base (info, context)
        {
        }
        #endregion

        [SecurityPermission (SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData (System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null)
                throw new ArgumentNullException ("info");

            ErrorCode = (ErrorCode) info.GetInt32 ("errorCode");
            ErrorText = info.GetString ("errorText");
            base.GetObjectData (info, context);
        }
    }
}
