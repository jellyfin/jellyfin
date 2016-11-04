using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Emby.Common.Implementations.Net
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
        /// Throws and <see cref="System.ObjectDisposedException"/> if the <see cref="IsDisposed"/> property is true.
        /// </summary>
        /// <seealso cref="IsDisposed"/>
        /// <exception cref="System.ObjectDisposedException">Thrown if the <see cref="IsDisposed"/> property is true.</exception>
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

        #region IDisposable Members

        /// <summary>
        /// Disposes this object instance and all internally managed resources.
        /// </summary>
        /// <remarks>
        /// <para>Sets the <see cref="IsDisposed"/> property to true. Does not explicitly throw an exception if called multiple times, but makes no promises about behaviour of derived classes.</para>
        /// </remarks>
        /// <seealso cref="IsDisposed"/>
        public void Dispose()
        {
            try
            {
                IsDisposed = true;

                Dispose(true);
            }
            finally
            {
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }
}
