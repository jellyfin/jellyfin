using System;
using System.Net;

namespace MediaBrowser.Dlna.Server
{
    public sealed class UpnpDevice
    {
        public readonly Uri Descriptor;
        public readonly string Type;
        public readonly string USN;
        public readonly string Uuid;
        public readonly IPAddress Address;

        public UpnpDevice(string aUuid, string aType, Uri aDescriptor, IPAddress address)
        {
            Uuid = aUuid;
            Type = aType;
            Descriptor = aDescriptor;

            Address = address;

            if (Type.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
            {
                USN = Type;
            }
            else
            {
                USN = String.Format("uuid:{0}::{1}", Uuid, Type);
            }
        }
    }
}
