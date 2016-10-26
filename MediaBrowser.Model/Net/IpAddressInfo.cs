using System;

namespace MediaBrowser.Model.Net
{
    public class IpAddressInfo
    {
        public string Address { get; set; }
        public bool IsIpv6 { get; set; }

        public override String ToString()
        {
            return Address;
        }
    }
}
