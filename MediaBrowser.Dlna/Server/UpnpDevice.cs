using System;

namespace MediaBrowser.Dlna.Server
{
    public sealed class UpnpDevice
    {
        public readonly Uri Descriptor;
        public readonly string Type;
        public readonly string USN;
        public readonly Guid Uuid;

        public UpnpDevice(Guid aUuid, string aType, Uri aDescriptor)
        {
            Uuid = aUuid;
            Type = aType;
            Descriptor = aDescriptor;

            if (Type.StartsWith("uuid:"))
            {
                USN = Type;
            }
            else
            {
                USN = String.Format("uuid:{0}::{1}", Uuid.ToString(), Type);
            }
        }
    }
}
