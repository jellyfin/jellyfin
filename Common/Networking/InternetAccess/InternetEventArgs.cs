using System;

namespace Common.Networking
{
    /// <summary>
    /// EventArgs class.
    /// </summary>
    public class InternetEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InternetEventArgs"/> class.
        /// </summary>
        /// <param name="state">Status of the event.</param>
        public InternetEventArgs(InternetState state)
        {
            Status = state;
        }

        /// <summary>
        /// Gets the status.
        /// </summary>
        public InternetState Status { get; }
    }
}
