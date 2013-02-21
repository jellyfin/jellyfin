using MediaBrowser.Common.Net;
using System;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Kernel
{
    /// <summary>
    /// Represents a class that is notified everytime the server receives a message over a WebSocket
    /// </summary>
    /// <typeparam name="TKernelType">The type of the T kernel type.</typeparam>
    public abstract class BaseWebSocketListener<TKernelType> : IWebSocketListener
           where TKernelType : IKernel
    {
        /// <summary>
        /// The null task result
        /// </summary>
        protected Task NullTaskResult = Task.FromResult(true);

        /// <summary>
        /// Gets the kernel.
        /// </summary>
        /// <value>The kernel.</value>
        protected TKernelType Kernel { get; private set; }

        /// <summary>
        /// Initializes the specified kernel.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        public virtual void Initialize(IKernel kernel)
        {
            if (kernel == null)
            {
                throw new ArgumentNullException("kernel");
            }

            Kernel = (TKernelType)kernel;
        }

        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException">message</exception>
        public Task ProcessMessage(WebSocketMessageInfo message)
        {
            if (message == null)
            {
                throw new ArgumentNullException("message");
            }

            return ProcessMessageInternal(message);
        }

        /// <summary>
        /// Processes the message internal.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        protected abstract Task ProcessMessageInternal(WebSocketMessageInfo message);

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

    /// <summary>
    /// Interface IWebSocketListener
    /// </summary>
    public interface IWebSocketListener : IDisposable
    {
        /// <summary>
        /// Processes the message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <returns>Task.</returns>
        Task ProcessMessage(WebSocketMessageInfo message);

        /// <summary>
        /// Initializes the specified kernel.
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        void Initialize(IKernel kernel);
    }
}
