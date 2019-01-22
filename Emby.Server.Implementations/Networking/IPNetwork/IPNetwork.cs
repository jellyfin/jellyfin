using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Text.RegularExpressions;

namespace System.Net
{
    /// <summary>
    /// IP Network utility class.
    /// Use IPNetwork.Parse to create instances.
    /// </summary>
    public class IPNetwork : IComparable<IPNetwork>
    {

        #region properties

        //private uint _network;
        private BigInteger _ipaddress;
        private AddressFamily _family;
        //private uint _netmask;
        //private uint _broadcast;
        //private uint _firstUsable;
        //private uint _lastUsable;
        //private uint _usable;
        private byte _cidr;

        #endregion

        #region accessors

        private BigInteger _network
        {
            get
            {
                var uintNetwork = this._ipaddress & this._netmask;
                return uintNetwork;
            }
        }

        /// <summary>
        /// Network address
        /// </summary>
        public IPAddress Network => IPNetwork.ToIPAddress(this._network, this._family);

        /// <summary>
        /// Address Family
        /// </summary>
        public AddressFamily AddressFamily => this._family;

        private BigInteger _netmask => IPNetwork.ToUint(this._cidr, this._family);

        /// <summary>
        /// Netmask
        /// </summary>
        public IPAddress Netmask => IPNetwork.ToIPAddress(this._netmask, this._family);

        private BigInteger _broadcast
        {
            get
            {

                int width = this._family == Sockets.AddressFamily.InterNetwork ? 4 : 16;
                var uintBroadcast = this._network + this._netmask.PositiveReverse(width);
                return uintBroadcast;
            }
        }

        /// <summary>
        /// Broadcast address
        /// </summary>
        public IPAddress Broadcast
        {
            get
            {
                if (this._family == Sockets.AddressFamily.InterNetworkV6)
                {
                    return null;
                }
                return IPNetwork.ToIPAddress(this._broadcast, this._family);
            }
        }

        /// <summary>
        /// First usable IP adress in Network
        /// </summary>
        public IPAddress FirstUsable
        {
            get
            {
                var fisrt = this._family == Sockets.AddressFamily.InterNetworkV6
                    ? this._network
                    : (this.Usable <= 0) ? this._network : this._network + 1;
                return IPNetwork.ToIPAddress(fisrt, this._family);
            }
        }

        /// <summary>
        /// Last usable IP adress in Network
        /// </summary>
        public IPAddress LastUsable
        {
            get
            {
                var last = this._family == Sockets.AddressFamily.InterNetworkV6
                    ? this._broadcast
                    : (this.Usable <= 0) ? this._network : this._broadcast - 1;
                return IPNetwork.ToIPAddress(last, this._family);
            }
        }

        /// <summary>
        /// Number of usable IP adress in Network
        /// </summary>
        public BigInteger Usable
        {
            get
            {

                if (this._family == Sockets.AddressFamily.InterNetworkV6)
                {
                    return this.Total;
                }
                byte[] mask = new byte[] { 0xff, 0xff, 0xff, 0xff, 0x00 };
                var bmask = new BigInteger(mask);
                var usableIps = (_cidr > 30) ? 0 : ((bmask >> _cidr) - 1);
                return usableIps;
            }
        }

        /// <summary>
        /// Number of IP adress in Network
        /// </summary>
        public BigInteger Total
        {
            get
            {

                int max = this._family == Sockets.AddressFamily.InterNetwork ? 32 : 128;
                var count = BigInteger.Pow(2, (max - _cidr));
                return count;
            }
        }


        /// <summary>
        /// The CIDR netmask notation
        /// </summary>
        public byte Cidr => this._cidr;

        #endregion

        #region constructor

#if TRAVISCI
        public
#else
        internal
#endif

        IPNetwork(BigInteger ipaddress, AddressFamily family, byte cidr)
        {

            int maxCidr = family == Sockets.AddressFamily.InterNetwork ? 32 : 128;
            if (cidr > maxCidr)
            {
                throw new ArgumentOutOfRangeException(nameof(cidr));
            }

            this._ipaddress = ipaddress;
            this._family = family;
            this._cidr = cidr;

        }

        #endregion

        #region parsers

        /// <summary>
        /// 192.168.168.100 - 255.255.255.0
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static IPNetwork Parse(string ipaddress, string netmask)
        {

            IPNetwork ipnetwork = null;
            IPNetwork.InternalParse(false, ipaddress, netmask, out ipnetwork);
            return ipnetwork;
        }

        /// <summary>
        /// 192.168.168.100/24
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static IPNetwork Parse(string ipaddress, byte cidr)
        {

            IPNetwork ipnetwork = null;
            IPNetwork.InternalParse(false, ipaddress, cidr, out ipnetwork);
            return ipnetwork;

        }

        /// <summary>
        /// 192.168.168.100 255.255.255.0
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static IPNetwork Parse(IPAddress ipaddress, IPAddress netmask)
        {

            IPNetwork ipnetwork = null;
            IPNetwork.InternalParse(false, ipaddress, netmask, out ipnetwork);
            return ipnetwork;

        }

        /// <summary>
        /// 192.168.0.1/24
        /// 192.168.0.1 255.255.255.0
        ///
        /// Network   : 192.168.0.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.0.1
        /// End       : 192.168.0.254
        /// Broadcast : 192.168.0.255
        /// </summary>
        /// <param name="network"></param>
        /// <returns></returns>
        public static IPNetwork Parse(string network)
        {

            IPNetwork ipnetwork = null;
            IPNetwork.InternalParse(false, network, out ipnetwork);
            return ipnetwork;

        }

        #endregion

        #region TryParse



        /// <summary>
        /// 192.168.168.100 - 255.255.255.0
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static bool TryParse(string ipaddress, string netmask, out IPNetwork ipnetwork)
        {

            IPNetwork ipnetwork2 = null;
            IPNetwork.InternalParse(true, ipaddress, netmask, out ipnetwork2);
            bool parsed = (ipnetwork2 != null);
            ipnetwork = ipnetwork2;
            return parsed;

        }



        /// <summary>
        /// 192.168.168.100/24
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static bool TryParse(string ipaddress, byte cidr, out IPNetwork ipnetwork)
        {

            IPNetwork ipnetwork2 = null;
            IPNetwork.InternalParse(true, ipaddress, cidr, out ipnetwork2);
            bool parsed = (ipnetwork2 != null);
            ipnetwork = ipnetwork2;
            return parsed;

        }

        /// <summary>
        /// 192.168.0.1/24
        /// 192.168.0.1 255.255.255.0
        ///
        /// Network   : 192.168.0.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.0.1
        /// End       : 192.168.0.254
        /// Broadcast : 192.168.0.255
        /// </summary>
        /// <param name="network"></param>
        /// <param name="ipnetwork"></param>
        /// <returns></returns>
        public static bool TryParse(string network, out IPNetwork ipnetwork)
        {

            IPNetwork ipnetwork2 = null;
            IPNetwork.InternalParse(true, network, out ipnetwork2);
            bool parsed = (ipnetwork2 != null);
            ipnetwork = ipnetwork2;
            return parsed;

        }

        /// <summary>
        /// 192.168.0.1/24
        /// 192.168.0.1 255.255.255.0
        ///
        /// Network   : 192.168.0.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.0.1
        /// End       : 192.168.0.254
        /// Broadcast : 192.168.0.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <param name="ipnetwork"></param>
        /// <returns></returns>
        public static bool TryParse(IPAddress ipaddress, IPAddress netmask, out IPNetwork ipnetwork)
        {

            IPNetwork ipnetwork2 = null;
            IPNetwork.InternalParse(true, ipaddress, netmask, out ipnetwork2);
            bool parsed = (ipnetwork2 != null);
            ipnetwork = ipnetwork2;
            return parsed;

        }


        #endregion

        #region InternalParse

        /// <summary>
        /// 192.168.168.100 - 255.255.255.0
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <returns></returns>
        private static void InternalParse(bool tryParse, string ipaddress, string netmask, out IPNetwork ipnetwork)
        {

            if (string.IsNullOrEmpty(ipaddress))
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(ipaddress));
                }
                ipnetwork = null;
                return;
            }

            if (string.IsNullOrEmpty(netmask))
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(netmask));
                }
                ipnetwork = null;
                return;
            }

            IPAddress ip = null;
            bool ipaddressParsed = IPAddress.TryParse(ipaddress, out ip);
            if (ipaddressParsed == false)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("ipaddress");
                }
                ipnetwork = null;
                return;
            }

            IPAddress mask = null;
            bool netmaskParsed = IPAddress.TryParse(netmask, out mask);
            if (netmaskParsed == false)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("netmask");
                }
                ipnetwork = null;
                return;
            }

            IPNetwork.InternalParse(tryParse, ip, mask, out ipnetwork);
        }

        private static void InternalParse(bool tryParse, string network, out IPNetwork ipnetwork)
        {

            if (string.IsNullOrEmpty(network))
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(network));
                }
                ipnetwork = null;
                return;
            }

            network = Regex.Replace(network, @"[^0-9a-fA-F\.\/\s\:]+", "");
            network = Regex.Replace(network, @"\s{2,}", " ");
            network = network.Trim();
            string[] args = network.Split(new char[] { ' ', '/' });
            byte cidr = 0;
            if (args.Length == 1)
            {

                if (IPNetwork.TryGuessCidr(args[0], out cidr))
                {
                    IPNetwork.InternalParse(tryParse, args[0], cidr, out ipnetwork);
                    return;
                }

                if (tryParse == false)
                {
                    throw new ArgumentException("network");
                }
                ipnetwork = null;
                return;
            }

            if (byte.TryParse(args[1], out cidr))
            {
                IPNetwork.InternalParse(tryParse, args[0], cidr, out ipnetwork);
                return;
            }

            IPNetwork.InternalParse(tryParse, args[0], args[1], out ipnetwork);
            return;

        }



        /// <summary>
        /// 192.168.168.100 255.255.255.0
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="netmask"></param>
        /// <returns></returns>
        private static void InternalParse(bool tryParse, IPAddress ipaddress, IPAddress netmask, out IPNetwork ipnetwork)
        {

            if (ipaddress == null)
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(ipaddress));
                }
                ipnetwork = null;
                return;
            }

            if (netmask == null)
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(netmask));
                }
                ipnetwork = null;
                return;
            }

            var uintIpAddress = IPNetwork.ToBigInteger(ipaddress);
            bool parsed = IPNetwork.TryToCidr(netmask, out var cidr2);
            if (parsed == false)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("netmask");
                }
                ipnetwork = null;
                return;
            }
            byte cidr = (byte)cidr2;

            var ipnet = new IPNetwork(uintIpAddress, ipaddress.AddressFamily, cidr);
            ipnetwork = ipnet;

            return;
        }



        /// <summary>
        /// 192.168.168.100/24
        ///
        /// Network   : 192.168.168.0
        /// Netmask   : 255.255.255.0
        /// Cidr      : 24
        /// Start     : 192.168.168.1
        /// End       : 192.168.168.254
        /// Broadcast : 192.168.168.255
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <param name="cidr"></param>
        /// <returns></returns>
        private static void InternalParse(bool tryParse, string ipaddress, byte cidr, out IPNetwork ipnetwork)
        {

            if (string.IsNullOrEmpty(ipaddress))
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(ipaddress));
                }
                ipnetwork = null;
                return;
            }


            IPAddress ip = null;
            bool ipaddressParsed = IPAddress.TryParse(ipaddress, out ip);
            if (ipaddressParsed == false)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("ipaddress");
                }
                ipnetwork = null;
                return;
            }

            IPAddress mask = null;
            bool parsedNetmask = IPNetwork.TryToNetmask(cidr, ip.AddressFamily, out mask);
            if (parsedNetmask == false)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("cidr");
                }
                ipnetwork = null;
                return;
            }


            IPNetwork.InternalParse(tryParse, ip, mask, out ipnetwork);
        }

        #endregion

        #region converters

        #region ToUint

        /// <summary>
        /// Convert an ipadress to decimal
        /// 0.0.0.0 -> 0
        /// 0.0.1.0 -> 256
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        public static BigInteger ToBigInteger(IPAddress ipaddress)
        {
            IPNetwork.InternalToBigInteger(false, ipaddress, out var uintIpAddress);
            return (BigInteger)uintIpAddress;

        }

        /// <summary>
        /// Convert an ipadress to decimal
        /// 0.0.0.0 -> 0
        /// 0.0.1.0 -> 256
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        public static bool TryToBigInteger(IPAddress ipaddress, out BigInteger? uintIpAddress)
        {
            IPNetwork.InternalToBigInteger(true, ipaddress, out var uintIpAddress2);
            bool parsed = (uintIpAddress2 != null);
            uintIpAddress = uintIpAddress2;
            return parsed;
        }

#if TRAVISCI
        public
#else
        internal
#endif
            static void InternalToBigInteger(bool tryParse, IPAddress ipaddress, out BigInteger? uintIpAddress)
        {

            if (ipaddress == null)
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(ipaddress));
                }
                uintIpAddress = null;
                return;
            }

            byte[] bytes = ipaddress.GetAddressBytes();
            /// 20180217 lduchosal
            /// code impossible to reach, GetAddressBytes returns either 4 or 16 bytes length addresses
            /// if (bytes.Length != 4 && bytes.Length != 16) {
            ///     if (tryParse == false) {
            ///         throw new ArgumentException("bytes");
            ///     }
            ///     uintIpAddress = null;
            ///     return;
            /// }

            Array.Reverse(bytes);
            var unsigned = new List<byte>(bytes);
            unsigned.Add(0);
            uintIpAddress = new BigInteger(unsigned.ToArray());
            return;
        }


        /// <summary>
        /// Convert a cidr to BigInteger netmask
        /// </summary>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static BigInteger ToUint(byte cidr, AddressFamily family)
        {
            IPNetwork.InternalToBigInteger(false, cidr, family, out var uintNetmask);
            return (BigInteger)uintNetmask;
        }


        /// <summary>
        /// Convert a cidr to uint netmask
        /// </summary>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static bool TryToUint(byte cidr, AddressFamily family, out BigInteger? uintNetmask)
        {
            IPNetwork.InternalToBigInteger(true, cidr, family, out var uintNetmask2);
            bool parsed = (uintNetmask2 != null);
            uintNetmask = uintNetmask2;
            return parsed;
        }

        /// <summary>
        /// Convert a cidr to uint netmask
        /// </summary>
        /// <param name="cidr"></param>
        /// <returns></returns>
#if TRAVISCI
        public
#else
        internal
#endif
            static void InternalToBigInteger(bool tryParse, byte cidr, AddressFamily family, out BigInteger? uintNetmask)
        {

            if (family == AddressFamily.InterNetwork && cidr > 32)
            {
                if (tryParse == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(cidr));
                }
                uintNetmask = null;
                return;
            }

            if (family == AddressFamily.InterNetworkV6 && cidr > 128)
            {
                if (tryParse == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(cidr));
                }
                uintNetmask = null;
                return;
            }

            if (family != AddressFamily.InterNetwork
                && family != AddressFamily.InterNetworkV6)
            {
                if (tryParse == false)
                {
                    throw new NotSupportedException(family.ToString());
                }
                uintNetmask = null;
                return;
            }

            if (family == AddressFamily.InterNetwork)
            {

                uintNetmask = cidr == 0 ? 0 : 0xffffffff << (32 - cidr);
                return;
            }

            var mask = new BigInteger(new byte[] {
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0x00
            });

            var masked = cidr == 0 ? 0 : mask << (128 - cidr);
            byte[] m = masked.ToByteArray();
            byte[] bmask = new byte[17];
            int copy = m.Length > 16 ? 16 : m.Length;
            Array.Copy(m, 0, bmask, 0, copy);
            uintNetmask = new BigInteger(bmask);


        }

        #endregion

        #region ToCidr

        /// <summary>
        /// Convert netmask to CIDR
        ///  255.255.255.0 -> 24
        ///  255.255.0.0   -> 16
        ///  255.0.0.0     -> 8
        /// </summary>
        /// <param name="netmask"></param>
        /// <returns></returns>
        private static void InternalToCidr(bool tryParse, BigInteger netmask, AddressFamily family, out byte? cidr)
        {

            if (!IPNetwork.InternalValidNetmask(netmask, family))
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("netmask");
                }
                cidr = null;
                return;
            }

            byte cidr2 = IPNetwork.BitsSet(netmask, family);
            cidr = cidr2;
            return;

        }
        /// <summary>
        /// Convert netmask to CIDR
        ///  255.255.255.0 -> 24
        ///  255.255.0.0   -> 16
        ///  255.0.0.0     -> 8
        /// </summary>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static byte ToCidr(IPAddress netmask)
        {
            IPNetwork.InternalToCidr(false, netmask, out var cidr);
            return (byte)cidr;
        }

        /// <summary>
        /// Convert netmask to CIDR
        ///  255.255.255.0 -> 24
        ///  255.255.0.0   -> 16
        ///  255.0.0.0     -> 8
        /// </summary>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static bool TryToCidr(IPAddress netmask, out byte? cidr)
        {
            IPNetwork.InternalToCidr(true, netmask, out var cidr2);
            bool parsed = (cidr2 != null);
            cidr = cidr2;
            return parsed;
        }

        private static void InternalToCidr(bool tryParse, IPAddress netmask, out byte? cidr)
        {

            if (netmask == null)
            {
                if (tryParse == false)
                {
                    throw new ArgumentNullException(nameof(netmask));
                }
                cidr = null;
                return;
            }

            bool parsed = IPNetwork.TryToBigInteger(netmask, out var uintNetmask2);

            /// 20180217 lduchosal
            /// impossible to reach code.
            /// if (parsed == false) {
            ///     if (tryParse == false) {
            ///         throw new ArgumentException("netmask");
            ///     }
            ///     cidr = null;
            ///     return;
            /// }
            var uintNetmask = (BigInteger)uintNetmask2;

            IPNetwork.InternalToCidr(tryParse, uintNetmask, netmask.AddressFamily, out var cidr2);
            cidr = cidr2;

            return;

        }


        #endregion

        #region ToNetmask

        /// <summary>
        /// Convert CIDR to netmask
        ///  24 -> 255.255.255.0
        ///  16 -> 255.255.0.0
        ///  8 -> 255.0.0.0
        /// </summary>
        /// <see cref="http://snipplr.com/view/15557/cidr-class-for-ipv4/"/>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static IPAddress ToNetmask(byte cidr, AddressFamily family)
        {

            IPAddress netmask = null;
            IPNetwork.InternalToNetmask(false, cidr, family, out netmask);
            return netmask;
        }

        /// <summary>
        /// Convert CIDR to netmask
        ///  24 -> 255.255.255.0
        ///  16 -> 255.255.0.0
        ///  8 -> 255.0.0.0
        /// </summary>
        /// <see cref="http://snipplr.com/view/15557/cidr-class-for-ipv4/"/>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static bool TryToNetmask(byte cidr, AddressFamily family, out IPAddress netmask)
        {

            IPAddress netmask2 = null;
            IPNetwork.InternalToNetmask(true, cidr, family, out netmask2);
            bool parsed = (netmask2 != null);
            netmask = netmask2;
            return parsed;
        }


#if TRAVISCI
        public
#else
        internal
#endif
            static void InternalToNetmask(bool tryParse, byte cidr, AddressFamily family, out IPAddress netmask)
        {

            if (family != AddressFamily.InterNetwork
                && family != AddressFamily.InterNetworkV6)
            {
                if (tryParse == false)
                {
                    throw new ArgumentException("family");
                }
                netmask = null;
                return;
            }

            /// 20180217 lduchosal
            /// impossible to reach code, byte cannot be negative :
            ///
            /// if (cidr < 0) {
            ///     if (tryParse == false) {
            ///         throw new ArgumentOutOfRangeException("cidr");
            ///     }
            ///     netmask = null;
            ///     return;
            /// }

            int maxCidr = family == Sockets.AddressFamily.InterNetwork ? 32 : 128;
            if (cidr > maxCidr)
            {
                if (tryParse == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(cidr));
                }
                netmask = null;
                return;
            }

            var mask = IPNetwork.ToUint(cidr, family);
            var netmask2 = IPNetwork.ToIPAddress(mask, family);
            netmask = netmask2;

            return;
        }

        #endregion

        #endregion

        #region utils

        #region BitsSet

        /// <summary>
        /// Count bits set to 1 in netmask
        /// </summary>
        /// <see cref="http://stackoverflow.com/questions/109023/best-algorithm-to-count-the-number-of-set-bits-in-a-32-bit-integer"/>
        /// <param name="netmask"></param>
        /// <returns></returns>
        private static byte BitsSet(BigInteger netmask, AddressFamily family)
        {

            string s = netmask.ToBinaryString();
            return (byte)s.Replace("0", "")
                .ToCharArray()
                .Length;

        }


        /// <summary>
        /// Count bits set to 1 in netmask
        /// </summary>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static uint BitsSet(IPAddress netmask)
        {
            var uintNetmask = IPNetwork.ToBigInteger(netmask);
            uint bits = IPNetwork.BitsSet(uintNetmask, netmask.AddressFamily);
            return bits;
        }

        #endregion

        #region ValidNetmask

        /// <summary>
        /// return true if netmask is a valid netmask
        /// 255.255.255.0, 255.0.0.0, 255.255.240.0, ...
        /// </summary>
        /// <see cref="http://www.actionsnip.com/snippets/tomo_atlacatl/calculate-if-a-netmask-is-valid--as2-"/>
        /// <param name="netmask"></param>
        /// <returns></returns>
        public static bool ValidNetmask(IPAddress netmask)
        {

            if (netmask == null)
            {
                throw new ArgumentNullException(nameof(netmask));
            }
            var uintNetmask = IPNetwork.ToBigInteger(netmask);
            bool valid = IPNetwork.InternalValidNetmask(uintNetmask, netmask.AddressFamily);
            return valid;
        }

#if TRAVISCI
        public
#else
        internal
#endif
            static bool InternalValidNetmask(BigInteger netmask, AddressFamily family)
        {

            if (family != AddressFamily.InterNetwork
                && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("family");
            }

            var mask = family == AddressFamily.InterNetwork
                ? new BigInteger(0x0ffffffff)
                : new BigInteger(new byte[]{
                    0xff, 0xff, 0xff, 0xff,
                    0xff, 0xff, 0xff, 0xff,
                    0xff, 0xff, 0xff, 0xff,
                    0xff, 0xff, 0xff, 0xff,
                    0x00
                });

            var neg = ((~netmask) & (mask));
            bool isNetmask = ((neg + 1) & neg) == 0;
            return isNetmask;

        }

        #endregion

        #region ToIPAddress

        /// <summary>
        /// Transform a uint ipaddress into IPAddress object
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        public static IPAddress ToIPAddress(BigInteger ipaddress, AddressFamily family)
        {

            int width = family == AddressFamily.InterNetwork ? 4 : 16;
            byte[] bytes = ipaddress.ToByteArray();
            byte[] bytes2 = new byte[width];
            int copy = bytes.Length > width ? width : bytes.Length;
            Array.Copy(bytes, 0, bytes2, 0, copy);
            Array.Reverse(bytes2);

            byte[] sized = Resize(bytes2, family);
            var ip = new IPAddress(sized);
            return ip;
        }

#if TRAVISCI
        public
#else
        internal
#endif
            static byte[] Resize(byte[] bytes, AddressFamily family)
        {

            if (family != AddressFamily.InterNetwork
                && family != AddressFamily.InterNetworkV6)
            {
                throw new ArgumentException("family");
            }

            int width = family == AddressFamily.InterNetwork ? 4 : 16;

            if (bytes.Length > width)
            {
                throw new ArgumentException("bytes");
            }

            byte[] result = new byte[width];
            Array.Copy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        #endregion

        #endregion

        #region contains

        /// <summary>
        /// return true if ipaddress is contained in network
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        public bool Contains(IPAddress ipaddress)
        {

            if (ipaddress == null)
            {
                throw new ArgumentNullException(nameof(ipaddress));
            }

            if (AddressFamily != ipaddress.AddressFamily)
            {
                return false;
            }

            var uintNetwork = _network;
            var uintBroadcast = _broadcast;
            var uintAddress = IPNetwork.ToBigInteger(ipaddress);

            bool contains = (uintAddress >= uintNetwork
                && uintAddress <= uintBroadcast);

            return contains;

        }

        /// <summary>
        /// return true is network2 is fully contained in network
        /// </summary>
        /// <param name="network2"></param>
        /// <returns></returns>
        public bool Contains(IPNetwork network2)
        {

            if (network2 == null)
            {
                throw new ArgumentNullException(nameof(network2));
            }

            var uintNetwork = _network;
            var uintBroadcast = _broadcast;

            var uintFirst = network2._network;
            var uintLast = network2._broadcast;

            bool contains = (uintFirst >= uintNetwork
                && uintLast <= uintBroadcast);

            return contains;
        }

        #endregion

        #region overlap

        /// <summary>
        /// return true is network2 overlap network
        /// </summary>
        /// <param name="network2"></param>
        /// <returns></returns>
        public bool Overlap(IPNetwork network2)
        {

            if (network2 == null)
            {
                throw new ArgumentNullException(nameof(network2));
            }

            var uintNetwork = _network;
            var uintBroadcast = _broadcast;

            var uintFirst = network2._network;
            var uintLast = network2._broadcast;

            bool overlap =
                (uintFirst >= uintNetwork && uintFirst <= uintBroadcast)
                || (uintLast >= uintNetwork && uintLast <= uintBroadcast)
                || (uintFirst <= uintNetwork && uintLast >= uintBroadcast)
                || (uintFirst >= uintNetwork && uintLast <= uintBroadcast);

            return overlap;
        }

        #endregion

        #region ToString

        public override string ToString()
        {
            return string.Format("{0}/{1}", this.Network, this.Cidr);
        }

        #endregion

        #region IANA block

        private static readonly Lazy<IPNetwork> _iana_ablock_reserved = new Lazy<IPNetwork>(() => IPNetwork.Parse("10.0.0.0/8"));
        private static readonly Lazy<IPNetwork> _iana_bblock_reserved = new Lazy<IPNetwork>(() => IPNetwork.Parse("172.16.0.0/12"));
        private static readonly Lazy<IPNetwork> _iana_cblock_reserved = new Lazy<IPNetwork>(() => IPNetwork.Parse("192.168.0.0/16"));

        /// <summary>
        /// 10.0.0.0/8
        /// </summary>
        /// <returns></returns>
        public static IPNetwork IANA_ABLK_RESERVED1 => _iana_ablock_reserved.Value;

        /// <summary>
        /// 172.12.0.0/12
        /// </summary>
        /// <returns></returns>
        public static IPNetwork IANA_BBLK_RESERVED1 => _iana_bblock_reserved.Value;

        /// <summary>
        /// 192.168.0.0/16
        /// </summary>
        /// <returns></returns>
        public static IPNetwork IANA_CBLK_RESERVED1 => _iana_cblock_reserved.Value;

        /// <summary>
        /// return true if ipaddress is contained in
        /// IANA_ABLK_RESERVED1, IANA_BBLK_RESERVED1, IANA_CBLK_RESERVED1
        /// </summary>
        /// <param name="ipaddress"></param>
        /// <returns></returns>
        public static bool IsIANAReserved(IPAddress ipaddress)
        {

            if (ipaddress == null)
            {
                throw new ArgumentNullException(nameof(ipaddress));
            }

            return IPNetwork.IANA_ABLK_RESERVED1.Contains(ipaddress)
                || IPNetwork.IANA_BBLK_RESERVED1.Contains(ipaddress)
                || IPNetwork.IANA_CBLK_RESERVED1.Contains(ipaddress);
        }

        /// <summary>
        /// return true if ipnetwork is contained in
        /// IANA_ABLK_RESERVED1, IANA_BBLK_RESERVED1, IANA_CBLK_RESERVED1
        /// </summary>
        /// <returns></returns>
        public bool IsIANAReserved()
        {
            return IPNetwork.IANA_ABLK_RESERVED1.Contains(this)
                || IPNetwork.IANA_BBLK_RESERVED1.Contains(this)
                || IPNetwork.IANA_CBLK_RESERVED1.Contains(this);
        }

        #endregion

        #region Subnet

        /// <summary>
        /// Subnet a network into multiple nets of cidr mask
        /// Subnet 192.168.0.0/24 into cidr 25 gives 192.168.0.0/25, 192.168.0.128/25
        /// Subnet 10.0.0.0/8 into cidr 9 gives 10.0.0.0/9, 10.128.0.0/9
        /// </summary>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public IPNetworkCollection Subnet(byte cidr)
        {
            IPNetworkCollection ipnetworkCollection = null;
            IPNetwork.InternalSubnet(false, this, cidr, out ipnetworkCollection);
            return ipnetworkCollection;
        }

        /// <summary>
        /// Subnet a network into multiple nets of cidr mask
        /// Subnet 192.168.0.0/24 into cidr 25 gives 192.168.0.0/25, 192.168.0.128/25
        /// Subnet 10.0.0.0/8 into cidr 9 gives 10.0.0.0/9, 10.128.0.0/9
        /// </summary>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public bool TrySubnet(byte cidr, out IPNetworkCollection ipnetworkCollection)
        {
            IPNetworkCollection inc = null;
            IPNetwork.InternalSubnet(true, this, cidr, out inc);
            if (inc == null)
            {
                ipnetworkCollection = null;
                return false;
            }

            ipnetworkCollection = inc;
            return true;
        }

#if TRAVISCI
        public
#else
        internal
#endif
            static void InternalSubnet(bool trySubnet, IPNetwork network, byte cidr, out IPNetworkCollection ipnetworkCollection)
        {

            if (network == null)
            {
                if (trySubnet == false)
                {
                    throw new ArgumentNullException(nameof(network));
                }
                ipnetworkCollection = null;
                return;
            }

            int maxCidr = network._family == Sockets.AddressFamily.InterNetwork ? 32 : 128;
            if (cidr > maxCidr)
            {
                if (trySubnet == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(cidr));
                }
                ipnetworkCollection = null;
                return;
            }

            if (cidr < network.Cidr)
            {
                if (trySubnet == false)
                {
                    throw new ArgumentException("cidr");
                }
                ipnetworkCollection = null;
                return;
            }

            ipnetworkCollection = new IPNetworkCollection(network, cidr);
            return;
        }



        #endregion

        #region Supernet

        /// <summary>
        /// Supernet two consecutive cidr equal subnet into a single one
        /// 192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
        /// 10.1.0.0/16 + 10.0.0.0/16 = 10.0.0.0/15
        /// 192.168.0.0/24 + 192.168.0.0/25 = 192.168.0.0/24
        /// </summary>
        /// <param name="network2"></param>
        /// <returns></returns>
        public IPNetwork Supernet(IPNetwork network2)
        {
            IPNetwork supernet = null;
            IPNetwork.InternalSupernet(false, this, network2, out supernet);
            return supernet;
        }

        /// <summary>
        /// Try to supernet two consecutive cidr equal subnet into a single one
        /// 192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
        /// 10.1.0.0/16 + 10.0.0.0/16 = 10.0.0.0/15
        /// 192.168.0.0/24 + 192.168.0.0/25 = 192.168.0.0/24
        /// </summary>
        /// <param name="network2"></param>
        /// <returns></returns>
        public bool TrySupernet(IPNetwork network2, out IPNetwork supernet)
        {

            IPNetwork outSupernet = null;
            IPNetwork.InternalSupernet(true, this, network2, out outSupernet);
            bool parsed = (outSupernet != null);
            supernet = outSupernet;
            return parsed;
        }

#if TRAVISCI
        public
#else
        internal
#endif
            static void InternalSupernet(bool trySupernet, IPNetwork network1, IPNetwork network2, out IPNetwork supernet)
        {

            if (network1 == null)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentNullException(nameof(network1));
                }
                supernet = null;
                return;
            }

            if (network2 == null)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentNullException(nameof(network2));
                }
                supernet = null;
                return;
            }


            if (network1.Contains(network2))
            {
                supernet = new IPNetwork(network1._network, network1._family, network1.Cidr);
                return;
            }

            if (network2.Contains(network1))
            {
                supernet = new IPNetwork(network2._network, network2._family, network2.Cidr);
                return;
            }

            if (network1._cidr != network2._cidr)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentException("cidr");
                }
                supernet = null;
                return;
            }

            var first = (network1._network < network2._network) ? network1 : network2;
            var last = (network1._network > network2._network) ? network1 : network2;

            /// Starting from here :
            /// network1 and network2 have the same cidr,
            /// network1 does not contain network2,
            /// network2 does not contain network1,
            /// first is the lower subnet
            /// last is the higher subnet


            if ((first._broadcast + 1) != last._network)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentOutOfRangeException(nameof(trySupernet), "TrySupernet was false while the first and last networks are not adjacent.");
                }
                supernet = null;
                return;
            }

            var uintSupernet = first._network;
            byte cidrSupernet = (byte)(first._cidr - 1);

            var networkSupernet = new IPNetwork(uintSupernet, first._family, cidrSupernet);
            if (networkSupernet._network != first._network)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentException("network");
                }
                supernet = null;
                return;
            }
            supernet = networkSupernet;
            return;
        }

        #endregion

        #region GetHashCode

        public override int GetHashCode()
        {
            return string.Format("{0}|{1}|{2}",
                this._ipaddress.GetHashCode(),
                this._network.GetHashCode(),
                this._cidr.GetHashCode()).GetHashCode();
        }

        #endregion

        #region SupernetArray

        /// <summary>
        /// Supernet a list of subnet
        /// 192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
        /// 192.168.0.0/24 + 192.168.1.0/24 + 192.168.2.0/24 + 192.168.3.0/24 = 192.168.0.0/22
        /// </summary>
        /// <param name="ipnetworks">The IP networks</param>
        /// <returns></returns>
        public static IPNetwork[] Supernet(IPNetwork[] ipnetworks)
        {
            InternalSupernet(false, ipnetworks, out var supernet);
            return supernet;
        }

        /// <summary>
        /// Supernet a list of subnet
        /// 192.168.0.0/24 + 192.168.1.0/24 = 192.168.0.0/23
        /// 192.168.0.0/24 + 192.168.1.0/24 + 192.168.2.0/24 + 192.168.3.0/24 = 192.168.0.0/22
        /// </summary>
        /// <param name="ipnetworks"></param>
        /// <param name="supernet"></param>
        /// <returns></returns>
        public static bool TrySupernet(IPNetwork[] ipnetworks, out IPNetwork[] supernet)
        {
            bool supernetted = InternalSupernet(true, ipnetworks, out supernet);
            return supernetted;

        }

#if TRAVISCI
        public
#else
        internal
#endif
        static bool InternalSupernet(bool trySupernet, IPNetwork[] ipnetworks, out IPNetwork[] supernet)
        {

            if (ipnetworks == null)
            {
                if (trySupernet == false)
                {
                    throw new ArgumentNullException(nameof(ipnetworks));
                }
                supernet = null;
                return false;
            }

            if (ipnetworks.Length <= 0)
            {
                supernet = new IPNetwork[0];
                return true;
            }

            var supernetted = new List<IPNetwork>();
            var ipns = IPNetwork.Array2List(ipnetworks);
            var current = IPNetwork.List2Stack(ipns);
            int previousCount = 0;
            int currentCount = current.Count;

            while (previousCount != currentCount)
            {

                supernetted.Clear();
                while (current.Count > 1)
                {
                    var ipn1 = current.Pop();
                    var ipn2 = current.Peek();

                    IPNetwork outNetwork = null;
                    bool success = ipn1.TrySupernet(ipn2, out outNetwork);
                    if (success)
                    {
                        current.Pop();
                        current.Push(outNetwork);
                    }
                    else
                    {
                        supernetted.Add(ipn1);
                    }
                }
                if (current.Count == 1)
                {
                    supernetted.Add(current.Pop());
                }

                previousCount = currentCount;
                currentCount = supernetted.Count;
                current = IPNetwork.List2Stack(supernetted);

            }
            supernet = supernetted.ToArray();
            return true;
        }

        private static Stack<IPNetwork> List2Stack(List<IPNetwork> list)
        {
            var stack = new Stack<IPNetwork>();
            list.ForEach(new Action<IPNetwork>(
                delegate (IPNetwork ipn)
                {
                    stack.Push(ipn);
                }
            ));
            return stack;
        }

        private static List<IPNetwork> Array2List(IPNetwork[] array)
        {
            var ipns = new List<IPNetwork>();
            ipns.AddRange(array);
            IPNetwork.RemoveNull(ipns);
            ipns.Sort(new Comparison<IPNetwork>(
                delegate (IPNetwork ipn1, IPNetwork ipn2)
                {
                    int networkCompare = ipn1._network.CompareTo(ipn2._network);
                    if (networkCompare == 0)
                    {
                        int cidrCompare = ipn1._cidr.CompareTo(ipn2._cidr);
                        return cidrCompare;
                    }
                    return networkCompare;
                }
            ));
            ipns.Reverse();

            return ipns;
        }

        private static void RemoveNull(List<IPNetwork> ipns)
        {
            ipns.RemoveAll(new Predicate<IPNetwork>(
                delegate (IPNetwork ipn)
                {
                    if (ipn == null)
                    {
                        return true;
                    }
                    return false;
                }
            ));

        }

        #endregion

        #region WideSubnet

        public static IPNetwork WideSubnet(string start, string end)
        {

            if (string.IsNullOrEmpty(start))
            {
                throw new ArgumentNullException(nameof(start));
            }

            if (string.IsNullOrEmpty(end))
            {
                throw new ArgumentNullException(nameof(end));
            }

            if (!IPAddress.TryParse(start, out var startIP))
            {
                throw new ArgumentException("start");
            }

            if (!IPAddress.TryParse(end, out var endIP))
            {
                throw new ArgumentException("end");
            }

            if (startIP.AddressFamily != endIP.AddressFamily)
            {
                throw new NotSupportedException("MixedAddressFamily");
            }

            var ipnetwork = new IPNetwork(0, startIP.AddressFamily, 0);
            for (byte cidr = 32; cidr >= 0; cidr--)
            {
                var wideSubnet = IPNetwork.Parse(start, cidr);
                if (wideSubnet.Contains(endIP))
                {
                    ipnetwork = wideSubnet;
                    break;
                }
            }
            return ipnetwork;

        }

        public static bool TryWideSubnet(IPNetwork[] ipnetworks, out IPNetwork ipnetwork)
        {
            IPNetwork ipn = null;
            IPNetwork.InternalWideSubnet(true, ipnetworks, out ipn);
            if (ipn == null)
            {
                ipnetwork = null;
                return false;
            }
            ipnetwork = ipn;
            return true;
        }

        public static IPNetwork WideSubnet(IPNetwork[] ipnetworks)
        {
            IPNetwork ipn = null;
            IPNetwork.InternalWideSubnet(false, ipnetworks, out ipn);
            return ipn;
        }

        internal static void InternalWideSubnet(bool tryWide, IPNetwork[] ipnetworks, out IPNetwork ipnetwork)
        {

            if (ipnetworks == null)
            {
                if (tryWide == false)
                {
                    throw new ArgumentNullException(nameof(ipnetworks));
                }
                ipnetwork = null;
                return;
            }


            IPNetwork[] nnin = Array.FindAll(ipnetworks, new Predicate<IPNetwork>(
                delegate (IPNetwork ipnet)
                {
                    return ipnet != null;
                }
            ));

            if (nnin.Length <= 0)
            {
                if (tryWide == false)
                {
                    throw new ArgumentException("ipnetworks");
                }
                ipnetwork = null;
                return;
            }

            if (nnin.Length == 1)
            {
                var ipn0 = nnin[0];
                ipnetwork = ipn0;
                return;
            }

            Array.Sort(nnin);
            var nnin0 = nnin[0];
            var uintNnin0 = nnin0._ipaddress;

            var nninX = nnin[nnin.Length - 1];
            var ipaddressX = nninX.Broadcast;

            var family = ipnetworks[0]._family;
            foreach (var ipnx in ipnetworks)
            {
                if (ipnx._family != family)
                {
                    throw new ArgumentException("MixedAddressFamily");
                }
            }

            var ipn = new IPNetwork(0, family, 0);
            for (byte cidr = nnin0._cidr; cidr >= 0; cidr--)
            {
                var wideSubnet = new IPNetwork(uintNnin0, family, cidr);
                if (wideSubnet.Contains(ipaddressX))
                {
                    ipn = wideSubnet;
                    break;
                }
            }

            ipnetwork = ipn;
            return;
        }

        #endregion

        #region Print

        /// <summary>
        /// Print an ipnetwork in a clear representation string
        /// </summary>
        /// <returns></returns>
        public string Print()
        {

            var sw = new StringWriter();

            sw.WriteLine("IPNetwork   : {0}", ToString());
            sw.WriteLine("Network     : {0}", Network);
            sw.WriteLine("Netmask     : {0}", Netmask);
            sw.WriteLine("Cidr        : {0}", Cidr);
            sw.WriteLine("Broadcast   : {0}", Broadcast);
            sw.WriteLine("FirstUsable : {0}", FirstUsable);
            sw.WriteLine("LastUsable  : {0}", LastUsable);
            sw.WriteLine("Usable      : {0}", Usable);

            return sw.ToString();
        }

        #endregion

        #region TryGuessCidr

        /// <summary>
        ///
        /// Class            Leading bits Default netmask
        ///     A (CIDR /8)  00           255.0.0.0
        ///     A (CIDR /8)  01           255.0.0.0
        ///     B (CIDR /16) 10           255.255.0.0
        ///     C (CIDR /24) 11           255.255.255.0
        ///
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static bool TryGuessCidr(string ip, out byte cidr)
        {

            IPAddress ipaddress = null;
            bool parsed = IPAddress.TryParse(string.Format("{0}", ip), out ipaddress);
            if (parsed == false)
            {
                cidr = 0;
                return false;
            }

            if (ipaddress.AddressFamily == AddressFamily.InterNetworkV6)
            {
                cidr = 64;
                return true;
            }
            var uintIPAddress = IPNetwork.ToBigInteger(ipaddress);
            uintIPAddress = uintIPAddress >> 29;
            if (uintIPAddress <= 3)
            {
                cidr = 8;
                return true;
            }
            else if (uintIPAddress <= 5)
            {
                cidr = 16;
                return true;
            }
            else if (uintIPAddress <= 6)
            {
                cidr = 24;
                return true;
            }

            cidr = 0;
            return false;

        }

        /// <summary>
        /// Try to parse cidr. Have to be >= 0 and <= 32 or 128
        /// </summary>
        /// <param name="sidr"></param>
        /// <param name="cidr"></param>
        /// <returns></returns>
        public static bool TryParseCidr(string sidr, AddressFamily family, out byte? cidr)
        {

            byte b = 0;
            if (!byte.TryParse(sidr, out b))
            {
                cidr = null;
                return false;
            }

            IPAddress netmask = null;
            if (!IPNetwork.TryToNetmask(b, family, out netmask))
            {
                cidr = null;
                return false;
            }

            cidr = b;
            return true;
        }

        #endregion

        #region ListIPAddress

        public IPAddressCollection ListIPAddress()
        {
            return new IPAddressCollection(this);
        }

        #endregion

        /**
         * Need a better way to do it
         *
#region TrySubstractNetwork

        public static bool TrySubstractNetwork(IPNetwork[] ipnetworks, IPNetwork substract, out IEnumerable<IPNetwork> result) {

            if (ipnetworks == null) {
                result = null;
                return false;
            }
            if (ipnetworks.Length <= 0) {
                result = null;
                return false;
            }
            if (substract == null) {
                result = null;
                return false;
            }
            var results = new List<IPNetwork>();
            foreach (var ipn in ipnetworks) {
                if (!Overlap(ipn, substract)) {
                    results.Add(ipn);
                    continue;
                }

                var collection = ipn.Subnet(substract.Cidr);
                var rtemp = new List<IPNetwork>();
                foreach(var subnet in collection) {
                    if (subnet != substract) {
                        rtemp.Add(subnet);
                    }
                }
                var supernets = Supernet(rtemp.ToArray());
                results.AddRange(supernets);
            }
            result = results;
            return true;
        }
#endregion
         * **/

        #region IComparable<IPNetwork> Members

        public static int Compare(IPNetwork left, IPNetwork right)
        {
            //  two null IPNetworks are equal
            if (ReferenceEquals(left, null) && ReferenceEquals(right, null)) return 0;

            //  two same IPNetworks are equal
            if (ReferenceEquals(left, right)) return 0;

            //  null is always sorted first
            if (ReferenceEquals(left, null)) return -1;
            if (ReferenceEquals(right, null)) return 1;

            //  first test the network
            var result = left._network.CompareTo(right._network);
            if (result != 0) return result;

            //  then test the cidr
            result = left._cidr.CompareTo(right._cidr);
            return result;
        }

        public int CompareTo(IPNetwork other)
        {
            return Compare(this, other);
        }

        public int CompareTo(object obj)
        {
            //  null is at less
            if (obj == null) return 1;

            //  convert to a proper Cidr object
            var other = obj as IPNetwork;

            //  type problem if null
            if (other == null)
            {
                throw new ArgumentException(
                    "The supplied parameter is an invalid type. Please supply an IPNetwork type.",
                    nameof(obj));
            }

            //  perform the comparision
            return CompareTo(other);
        }

        #endregion

        #region IEquatable<IPNetwork> Members

        public static bool Equals(IPNetwork left, IPNetwork right)
        {
            return Compare(left, right) == 0;
        }

        public bool Equals(IPNetwork other)
        {
            return Equals(this, other);
        }

        public override bool Equals(object obj)
        {
            return Equals(this, obj as IPNetwork);
        }

        #endregion

        #region Operators

        public static bool operator ==(IPNetwork left, IPNetwork right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(IPNetwork left, IPNetwork right)
        {
            return !Equals(left, right);
        }

        public static bool operator <(IPNetwork left, IPNetwork right)
        {
            return Compare(left, right) < 0;
        }

        public static bool operator >(IPNetwork left, IPNetwork right)
        {
            return Compare(left, right) > 0;
        }

        #endregion

    }
}
