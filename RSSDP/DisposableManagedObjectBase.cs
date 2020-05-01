using System;
using System.Collections.Generic;
using System.Text;

namespace Rssdp.Infrastructure
{
    /// <summary>
    /// Correclty implements the <see cref="IDisposable"/> interface and pattern for an object containing only managed resources, and adds a few common niceities not on the interface such as an <see cref="IsDisposed"/> property.
    /// </summary>
    public abstract class DisposableManagedObjectBase : IDisposable
    {

        #region Public Methods

        /// <summary>
        /// Override this method and dispose any objects you own the lifetime of if disposing is true;
        /// </summary>
        /// <param name="disposing">True if managed objects should be disposed, if false, only unmanaged resources should be released.</param>
        protected abstract void Dispose(bool disposing);

        /// <summary>
        /// Throws and <see cref="ObjectDisposedException"/> if the <see cref="IsDisposed"/> property is true.
        /// </summary>
        /// <seealso cref="IsDisposed"/>
        /// <exception cref="ObjectDisposedException">Thrown if the <see cref="IsDisposed"/> property is true.</exception>
        /// <seealso cref="Dispose()"/>
        protected virtual void ThrowIfDisposed()
        {
            if (this.IsDisposed) throw new ObjectDisposedException(this.GetType().FullName);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Sets or returns a boolean indicating whether or not this instance has been disposed.
        /// </summary>
        /// <seealso cref="Dispose()"/>
        public bool IsDisposed
        {
            get;
            private set;
        }

        #endregion

        public string BuildMessage(string header, Dictionary<string, string> values)
        {
            var builder = new StringBuilder();

            const string argFormat = "{0}: {1}\r\n";

            builder.AppendFormat("{0}\r\n", header);

            foreach (var pair in values)
            {
                builder.AppendFormat(argFormat, pair.Key, pair.Value);
            }

            builder.Append("\r\n");

            return builder.ToString();
        }

        #region IDisposable Members

        /// <summary>
        /// Disposes this object instance and all internally managed resources.
        /// </summary>
        /// <remarks>
        /// <para>Sets the <see cref="IsDisposed"/> property to true. Does not explicitly throw an exception if called multiple times, but makes no promises about behaviour of derived classes.</para>
        /// </remarks>
        /// <seealso cref="IsDisposed"/>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1063:ImplementIDisposableCorrectly", Justification = "We do exactly as asked, but CA doesn't seem to like us also setting the IsDisposed property. Too bad, it's a good idea and shouldn't cause an exception or anything likely to interfer with the dispose process.")]
        public void Dispose()
        {
            IsDisposed = true;

            Dispose(true);
        }

        #endregion
    }
}
