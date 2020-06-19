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

namespace Mono.Nat
{
    using System;
    using System.Security.Permissions;

    /// <summary>
    /// Defines the <see cref="MappingException" />.
    /// </summary>
    [Serializable]
    public class MappingException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class.
        /// </summary>
        /// <param name="errorCode">The errorCode<see cref="ErrorCode"/>.</param>
        /// <param name="errorText">The errorText<see cref="string"/>.</param>
        public MappingException(ErrorCode errorCode, string errorText)
            : base($"Error {errorCode}: {errorText}")
        {
            ErrorCode = errorCode;
            ErrorText = errorText;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class.
        /// </summary>
        public MappingException()
        {
            ErrorText = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class.
        /// </summary>
        /// <param name="message">The message<see cref="string"/>.</param>
        public MappingException(string message)
            : base(message)
        {
            ErrorText = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class.
        /// </summary>
        /// <param name="message">The message<see cref="string"/>.</param>
        /// <param name="innerException">The innerException<see cref="Exception"/>.</param>
        public MappingException(string message, Exception innerException)
            : base(message, innerException)
        {
            ErrorText = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MappingException"/> class.
        /// </summary>
        /// <param name="info">The info<see cref="System.Runtime.Serialization.SerializationInfo"/>.</param>
        /// <param name="context">The context<see cref="System.Runtime.Serialization.StreamingContext"/>.</param>
        protected MappingException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
            : base(info, context)
        {
            ErrorText = string.Empty;
        }

        /// <summary>
        /// Gets the ErrorCode.
        /// </summary>
        public ErrorCode ErrorCode { get; private set; } = ErrorCode.Unknown;

        /// <summary>
        /// Gets the ErrorText.
        /// </summary>
        public string ErrorText { get; private set; }

        /// <summary>
        /// The GetObjectData.
        /// </summary>
        /// <param name="info">The info<see cref="System.Runtime.Serialization.SerializationInfo"/>.</param>
        /// <param name="context">The context<see cref="System.Runtime.Serialization.StreamingContext"/>.</param>
        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            ErrorCode = (ErrorCode)info.GetInt32("errorCode");
            ErrorText = info.GetString("errorText");
            base.GetObjectData(info, context);
        }
    }
}
