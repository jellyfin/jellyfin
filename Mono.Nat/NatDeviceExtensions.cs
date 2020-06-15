namespace Mono.Nat
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="NatDeviceExtensions" />.
    /// </summary>
    public static class NatDeviceExtensions
    {
        /// <summary>
        /// The CreatePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping CreatePortMap(this INatDevice device, Mapping mapping)
        {
            return device.CreatePortMapAsync(mapping).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The DeletePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping DeletePortMap(this INatDevice device, Mapping mapping)
        {
            return device.DeletePortMapAsync(mapping).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The GetAllMappings.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <returns>The Mappings.</returns>
        public static Mapping[] GetAllMappings(this INatDevice device)
        {
            return device.GetAllMappingsAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// The GetExternalIP.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <returns>The <see cref="IPAddress"/>.</returns>
        public static IPAddress GetExternalIP(this INatDevice device)
        {
            return device.GetExternalIPAsync().GetAwaiter().GetResult();
        }

        /// <summary>
        /// The GetSpecificMapping.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="protocol">The protocol<see cref="Protocol"/>.</param>
        /// <param name="port">The port<see cref="int"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping GetSpecificMapping(this INatDevice device, Protocol protocol, int port)
        {
            return device.GetSpecificMappingAsync(protocol, port).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The BeginCreatePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        /// <returns>The <see cref="IAsyncResult"/>.</returns>
        public static IAsyncResult BeginCreatePortMap(this INatDevice device, Mapping mapping, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult(device.CreatePortMapAsync(mapping), callback, asyncState);
            result.Task.ContinueWith(t => result.Complete(), TaskScheduler.Default);
            return result;
        }

        /// <summary>
        /// The BeginDeletePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="mapping">The mapping<see cref="Mapping"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        /// <returns>The <see cref="IAsyncResult"/>.</returns>
        public static IAsyncResult BeginDeletePortMap(this INatDevice device, Mapping mapping, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult(device.DeletePortMapAsync(mapping), callback, asyncState);
            result.Task.ContinueWith(t => result.Complete(), TaskScheduler.Default);
            return result;
        }

        /// <summary>
        /// The EndDeletePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="result">The result<see cref="IAsyncResult"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping EndDeletePortMap(this INatDevice device, IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(result is TaskAsyncResult mappingResult))
            {
                throw new ArgumentException("Invalid AsyncResult", nameof(result));
            }

            return ((Task<Mapping>)mappingResult.Task).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The EndCreatePortMap.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="result">The result<see cref="IAsyncResult"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping EndCreatePortMap(this INatDevice device, IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(result is TaskAsyncResult mappingResult))
            {
                throw new ArgumentException("Invalid AsyncResult", nameof(result));
            }

            return ((Task<Mapping>)mappingResult.Task).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The EndGetSpecificMapping.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="result">The result<see cref="IAsyncResult"/>.</param>
        /// <returns>The <see cref="Mapping"/>.</returns>
        public static Mapping EndGetSpecificMapping(this INatDevice device, IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(result is TaskAsyncResult mappingResult))
            {
                throw new ArgumentException("Invalid AsyncResult", nameof(result));
            }

            return ((Task<Mapping>)mappingResult.Task).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The BeginGetAllMappings.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        /// <returns>The <see cref="IAsyncResult"/>.</returns>
        public static IAsyncResult BeginGetAllMappings(this INatDevice device, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult(device.GetAllMappingsAsync(), callback, asyncState);
            result.Task.ContinueWith(t => result.Complete(), TaskScheduler.Default);
            return result;
        }

        /// <summary>
        /// The EndGetAllMappings.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="result">The result<see cref="IAsyncResult"/>.</param>
        /// <returns>The Mapping.</returns>
        public static Mapping[] EndGetAllMappings(this INatDevice device, IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(result is TaskAsyncResult mappingResult))
            {
                throw new ArgumentException("Invalid AsyncResult", nameof(result));
            }

            return ((Task<Mapping[]>)mappingResult.Task).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The BeginGetExternalIP.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        /// <returns>The <see cref="IAsyncResult"/>.</returns>
        public static IAsyncResult BeginGetExternalIP(this INatDevice device, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult(device.GetExternalIPAsync(), callback, asyncState);
            result.Task.ContinueWith(t => result.Complete(), TaskScheduler.Default);
            return result;
        }

        /// <summary>
        /// The EndGetExternalIP.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="result">The result<see cref="IAsyncResult"/>.</param>
        /// <returns>The <see cref="IPAddress"/>.</returns>
        public static IPAddress EndGetExternalIP(this INatDevice device, IAsyncResult result)
        {
            if (result == null)
            {
                throw new ArgumentNullException(nameof(result));
            }

            if (!(result is TaskAsyncResult mappingResult))
            {
                throw new ArgumentException("Invalid AsyncResult", nameof(result));
            }

            return ((Task<IPAddress>)mappingResult.Task).GetAwaiter().GetResult();
        }

        /// <summary>
        /// The BeginGetSpecificMapping.
        /// </summary>
        /// <param name="device">The device<see cref="INatDevice"/>.</param>
        /// <param name="protocol">The protocol<see cref="Protocol"/>.</param>
        /// <param name="externalPort">The externalPort<see cref="int"/>.</param>
        /// <param name="callback">The callback<see cref="AsyncCallback"/>.</param>
        /// <param name="asyncState">The asyncState<see cref="object"/>.</param>
        /// <returns>The <see cref="IAsyncResult"/>.</returns>
        public static IAsyncResult BeginGetSpecificMapping(this INatDevice device, Protocol protocol, int externalPort, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult(device.GetSpecificMappingAsync(protocol, externalPort), callback, asyncState);
            result.Task.ContinueWith(t => result.Complete(), TaskScheduler.Default);
            return result;
        }
    }
}
