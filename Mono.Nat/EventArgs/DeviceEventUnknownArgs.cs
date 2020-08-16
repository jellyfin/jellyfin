using System;
using System.Net;

namespace Mono.Nat
{
    public class DeviceEventUnknownArgs : EventArgs
    {
        public IPAddress Address { get; }

        public string Data { get; }

        public EndPoint EndPoint { get; }

        public NatProtocol Protocol { get; }

        internal DeviceEventUnknownArgs (IPAddress address, EndPoint endPoint, string data, NatProtocol protocol)
        {
            Address = address;
            Data = data;
            EndPoint = endPoint;
            Protocol = protocol;
        }
    }
}
