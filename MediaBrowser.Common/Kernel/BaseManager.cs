using System;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Class BaseManager
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    public abstract class BaseManager<TKernelType> : IDisposable
        where TKernelType : class, IKernel
    {
        /// <summary>
        /// The _kernel
        /// </summary>
        protected readonly TKernelType Kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        /// <exception cref="System.ArgumentNullException">kernel</exception>
        protected BaseManager(TKernelType kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }
   
            Kernel = kernel;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
        }
    }
}
