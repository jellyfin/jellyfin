using MediaBrowser.Common;
using ServiceStack.Configuration;

namespace MediaBrowser.Server.Implementations.HttpServer
{
    /// <summary>
    /// Class ContainerAdapter
    /// </summary>
    class ContainerAdapter : IContainerAdapter, IRelease
    {
        /// <summary>
        /// The _app host
        /// </summary>
        private readonly IApplicationHost _appHost;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContainerAdapter" /> class.
        /// </summary>
        /// <param name="appHost">The app host.</param>
        public ContainerAdapter(IApplicationHost appHost)
        {
            _appHost = appHost;
        }
        /// <summary>
        /// Resolves this instance.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T Resolve<T>()
        {
            return _appHost.Resolve<T>();
        }

        /// <summary>
        /// Tries the resolve.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns>``0.</returns>
        public T TryResolve<T>()
        {
            return _appHost.TryResolve<T>();
        }

        /// <summary>
        /// Releases the specified instance.
        /// </summary>
        /// <param name="instance">The instance.</param>
        public void Release(object instance)
        {
            // Leave this empty so SS doesn't try to dispose our objects
        }
    }
}
