using System;
using System.Net;
using MediaBrowser.Model.Net;

namespace Emby.Dlna.Server
{
    public sealed class UpnpDevice
    {
        public readonly Uri Descriptor;
        public readonly string Type;
        public readonly string USN;
        public readonly string Uuid;
        public readonly IpAddressInfo Address;

        public UpnpDevice(string aUuid, string aType, Uri aDescriptor, IpAddressInfo address)
        {
            Uuid = aUuid;
            Type = aType;
            Descriptor = aDescriptor;

            Address = address;

            USN = CreateUSN(aUuid, aType);
        }

        private static string CreateUSN(string aUuid, string aType)
        {
            if (aType.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
            {
                return aType;
            }
            else
            {
                return String.Format("uuid:{0}::{1}", aUuid, aType);
            }
        }
    }
}
