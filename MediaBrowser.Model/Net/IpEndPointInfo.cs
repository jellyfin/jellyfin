using System;

namespace MediaBrowser.Model.Net
{
    public class IpEndPointInfo
    {
        public IpAddressInfo IpAddress { get; set; }

        public int Port { get; set; }

        public override string ToString()
        {
            var ipAddresString = IpAddress == null ? string.Empty : IpAddress.ToString();

            return ipAddresString + ":" + this.Port.ToString();
        }
    }
}
