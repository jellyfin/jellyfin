using System;
using System.Globalization;

namespace MediaBrowser.Model.Net
{
    public class IpEndPointInfo
    {
        public IpAddressInfo IpAddress { get; set; }

        public int Port { get; set; }

        public IpEndPointInfo()
        {

        }

        public IpEndPointInfo(IpAddressInfo address, int port)
        {
            IpAddress = address;
            Port = port;
        }

        public override string ToString()
        {
            var ipAddresString = IpAddress == null ? string.Empty : IpAddress.ToString();

            return ipAddresString + ":" + Port.ToString(CultureInfo.InvariantCulture);
        }
    }
}
