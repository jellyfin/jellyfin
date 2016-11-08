using System;

namespace MediaBrowser.Model.Net
{
    public class IpAddressInfo
    {
        public static IpAddressInfo Any = new IpAddressInfo("0.0.0.0", IpAddressFamily.InterNetwork);
        public static IpAddressInfo IPv6Any = new IpAddressInfo("00000000000000000000", IpAddressFamily.InterNetworkV6);
        public static IpAddressInfo Loopback = new IpAddressInfo("127.0.0.1", IpAddressFamily.InterNetwork);
        public static IpAddressInfo IPv6Loopback = new IpAddressInfo("::1", IpAddressFamily.InterNetworkV6);

        public string Address { get; set; }
        public IpAddressFamily AddressFamily { get; set; }

        public IpAddressInfo()
        {

        }

        public IpAddressInfo(string address, IpAddressFamily addressFamily)
        {
            Address = address;
            AddressFamily = addressFamily;
        }

        public bool Equals(IpAddressInfo address)
        {
            return string.Equals(address.Address, Address, StringComparison.OrdinalIgnoreCase);
        }

        public override String ToString()
        {
            return Address;
        }
    }

    public enum IpAddressFamily
    {
        InterNetwork,
        InterNetworkV6
    }
}
