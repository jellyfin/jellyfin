using System;
using System.Net;

namespace Mono.Nat
{
    public class DeviceEventUnknownArgs : EventArgs
    {
        public IPAddress Address { get; }

        public string Data { get; }

        public EndPoint EndPoint { get; }

        public DeviceEventUnknownArgs(IPAddress address, EndPoint endPoint, string data)
        {
            Address = address;
            Data = data;
            EndPoint = endPoint;
        }
    }
}
