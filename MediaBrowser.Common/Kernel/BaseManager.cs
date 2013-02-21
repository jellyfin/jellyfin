using MediaBrowser.Common.Logging;
using MediaBrowser.Model.Logging;
using System;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Class BaseManager
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    public abstract class BaseManager<TKernelType> : IDisposable
        where TKernelType : IKernel
    {
        /// <summary>
        /// Gets the logger.
        /// </summary>
        /// <value>The logger.</value>
        protected ILogger Logger { get; private set; }

        /// <summary>
        /// The _kernel
        /// </summary>
        protected readonly TKernelType Kernel;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseManager" /> class.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        protected BaseManager(TKernelType kernel)
        {
            Kernel = kernel;

            Logger = LogManager.GetLogger(GetType().Name);

            Logger.Info("Initializing");
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Logger.Info("Disposing");
            
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
