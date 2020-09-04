using System;
using System.Net;
using System.Threading.Tasks;

namespace Mono.Nat
{
    public static class NatDeviceExtensions
    {
        #region Synchronous methods

        public static Mapping CreatePortMap (this INatDevice device, Mapping mapping)
        {
            return device.CreatePortMapAsync (mapping).GetAwaiter ().GetResult ();
        }

        public static Mapping DeletePortMap (this INatDevice device, Mapping mapping)
        {
            return device.DeletePortMapAsync (mapping).GetAwaiter ().GetResult ();
        }

        public static Mapping[] GetAllMappings (this INatDevice device)
        {
            return device.GetAllMappingsAsync ().GetAwaiter ().GetResult ();
        }

        public static IPAddress GetExternalIP (this INatDevice device)
        {
            return device.GetExternalIPAsync ().GetAwaiter ().GetResult ();
        }

        public static Mapping GetSpecificMapping (this INatDevice device, Protocol protocol, int port)
        {
            return device.GetSpecificMappingAsync (protocol, port).GetAwaiter ().GetResult ();
        }

        #endregion Synchronous methods

        #region Old async methods

        public static IAsyncResult BeginCreatePortMap (this INatDevice device, Mapping mapping, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult (device.CreatePortMapAsync (mapping), callback, asyncState);
            result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
            return result;
        }

        public static Mapping EndCreatePortMap (this INatDevice device, IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException (nameof (result));

            if (!(result is TaskAsyncResult mappingResult))
                throw new ArgumentException ("Invalid AsyncResult", nameof (result));

            return ((Task<Mapping>) mappingResult.Task).GetAwaiter ().GetResult ();
        }

        public static IAsyncResult BeginDeletePortMap (this INatDevice device, Mapping mapping, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult (device.DeletePortMapAsync (mapping), callback, asyncState);
            result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
            return result;
        }

        public static Mapping EndDeletePortMap (this INatDevice device, IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException (nameof (result));

            if (!(result is TaskAsyncResult mappingResult))
                throw new ArgumentException ("Invalid AsyncResult", nameof (result));

            return ((Task<Mapping>) mappingResult.Task).GetAwaiter ().GetResult ();
        }

        public static IAsyncResult BeginGetAllMappings (this INatDevice device, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult (device.GetAllMappingsAsync (), callback, asyncState);
            result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
            return result;
        }

        public static Mapping[] EndGetAllMappings (this INatDevice device, IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException (nameof (result));

            if (!(result is TaskAsyncResult mappingResult))
                throw new ArgumentException ("Invalid AsyncResult", nameof (result));

            return ((Task<Mapping[]>) mappingResult.Task).GetAwaiter ().GetResult ();
        }

        public static IAsyncResult BeginGetExternalIP (this INatDevice device, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult (device.GetExternalIPAsync (), callback, asyncState);
            result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
            return result;
        }

        public static IPAddress EndGetExternalIP (this INatDevice device, IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException (nameof (result));

            if (!(result is TaskAsyncResult mappingResult))
                throw new ArgumentException ("Invalid AsyncResult", nameof (result));

            return ((Task<IPAddress>) mappingResult.Task).GetAwaiter ().GetResult ();
        }

        public static IAsyncResult BeginGetSpecificMapping (this INatDevice device, Protocol protocol, int externalPort, AsyncCallback callback, object asyncState)
        {
            var result = new TaskAsyncResult (device.GetSpecificMappingAsync (protocol, externalPort), callback, asyncState);
            result.Task.ContinueWith (t => result.Complete (), TaskScheduler.Default);
            return result;
        }

        public static Mapping EndGetSpecificMapping (this INatDevice device, IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException (nameof (result));

            if (!(result is TaskAsyncResult mappingResult))
                throw new ArgumentException ("Invalid AsyncResult", nameof (result));

            return ((Task<Mapping>) mappingResult.Task).GetAwaiter ().GetResult ();
        }

        #endregion Old async methods
    }
}
