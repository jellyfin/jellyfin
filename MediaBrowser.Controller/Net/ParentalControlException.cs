using System;

namespace MediaBrowser.Controller.Net
{
    /// <summary>
    /// The exception that is thrown when a user is refused because of parental control,
    /// such as being outside their configured access schedule.
    /// </summary>
    /// <remarks>
    /// Distinct from <see cref="SecurityException"/> so the API layer can tell clients
    /// <em>why</em> the request was refused. A client that only sees a bare 403 cannot
    /// distinguish this from an ordinary permission denial.
    /// </remarks>
    public class ParentalControlException : SecurityException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParentalControlException"/> class.
        /// </summary>
        public ParentalControlException()
            : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentalControlException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public ParentalControlException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParentalControlException"/> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public ParentalControlException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
