using System;

namespace MediaBrowser.Common.Extensions
{
    /// <summary>
    /// Class MethodNotAllowedException.
    /// </summary>
    [Serializable]
    public class MethodNotAllowedException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException" /> class.
        /// </summary>
        public MethodNotAllowedException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException" /> class.
        /// </summary>
        /// <param name="message">The message.</param>
        public MethodNotAllowedException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException"/> class.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public MethodNotAllowedException(string message, Exception innerException) : base(message, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MethodNotAllowedException"/> class.
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected MethodNotAllowedException(global::System.Runtime.Serialization.SerializationInfo serializationInfo, global::System.Runtime.Serialization.StreamingContext streamingContext)
        {
            throw new NotImplementedException();
        }
    }
}
