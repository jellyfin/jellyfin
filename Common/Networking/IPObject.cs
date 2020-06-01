using System;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Common.Networking
{
    /// <summary>
    /// Base network object class.
    /// </summary>
    public abstract class IPObject
    {
        private static readonly byte[] _ip6loopback = { 0, 0, 0, 0, 0, 0, 0, 1 };
        private static readonly byte[] _ip4loopback = { 127, 0, 0, 1 };

        /// <summary>
        /// Gets or sets the user defined functions that need storage in this object.
        /// </summary>
        public long Tag { get; set; }

        /// <summary>
        /// Tests to see if the ip address is an AIPIPA address. (169.254.x.x).
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsAIPIPA(IPAddress i)
        {
            if (i != null)
            {
                if (i.IsIPv6LinkLocal)
                {
                    return true;
                }

                byte[] b = i.GetAddressBytes();
                return b[0] == 169 && b[1] == 254;
            }

            return false;
        }

        /// <summary>
        /// Tests to see if the ip address is a Loopback address.
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsLoopback(IPAddress i)
        {
            if (i != null)
            {
                byte[] b = i.GetAddressBytes();
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    return CompareByteArray(b, _ip4loopback, 4);
                }
                else
                {
                    return CompareByteArray(b, _ip6loopback, 16);
                }
            }

            return false;
        }

        /// <summary>
        /// Tests to see if the ip address is an ip 6 address.
        /// </summary>
        /// <param name="i">Value to test.</param>
        /// <returns>True if it is.</returns>
        public static bool IsIP6(IPAddress i)
        {
            return (i != null) && (i.AddressFamily == AddressFamily.InterNetworkV6);
        }

        /// <summary>
        /// Tests to see if the address in i is in the private address ranges.
        /// </summary>
        /// <param name="i">Object to test.</param>
        /// <returns>True if it contains a private address.</returns>
        public static bool IsPrivateAddressRange(IPAddress i)
        {
            if (i != null)
            {
                if (i.AddressFamily == AddressFamily.InterNetwork)
                {
                    byte[] octet = i.GetAddressBytes();

                    return (octet[0] == 10) ||
                        (octet[0] == 172 && octet[1] >= 16 && octet[1] <= 31) || // RFC1918
                        (octet[0] == 192 && octet[1] == 168) || // RFC1918
                        (octet[0] == 127) || // RFC1122
                        (octet[0] == 169 && octet[1] == 254); // RFC3927
                }
                else
                {
                    if (i.IsIPv6SiteLocal)
                    {
                        return true;
                    }

                    byte[] octet = i.GetAddressBytes();

                    uint word = (uint)(octet[0] << 32) + octet[1];

                    return (word == 0xfc00 && word <= 0xfdff) // Unique local address.
                        || (word >= 0xfe80 && word <= 0xfebf) // Local link address.
                        || word == 0x100; // Discard prefix.
                }
            }

            return false;
        }

        /// <summary>
        /// Pings this object.
        /// </summary>
        /// <returns>The response of the ping.</returns>
        public async Task<bool> PingAsync()
        {
            IPAddress ip = GetAddressInternal();

            if (ip != null)
            {
                PingReply reply = await PingAsyncInternal(ip).ConfigureAwait(false);

                if (reply != null)
                {
                    return reply.Status == IPStatus.Success;
                }
            }

            return false;
        }

        /// <summary>
        /// Implements internal code for async callbacks.
        /// </summary>
        /// <param name="ip">IPAddress to pass.</param>
        /// <param name="callback">Delegate function.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>True to include, false to exclude.</returns>
        public static async Task<bool> CallbackAsyncInternal(IPAddress ip, Func<IPAddress, CancellationToken, Task<bool>> callback, CancellationToken ct)
        {
            try
            {
                return await callback(ip, ct).ConfigureAwait(false);
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch
#pragma warning restore CA1031 // Do not catch general exception types
            {
                return false;
            }
        }

        /// <summary>
        /// Implements code for async callbacks.
        /// </summary>
        /// <param name="callback">Function to call.</param>
        /// <param name="ct">Cancellation Tolken.</param>
        /// <returns>The response of the ping.</returns>
        public async Task<bool> CallbackAsync(Func<IPAddress, CancellationToken, Task<bool>> callback, CancellationToken ct)
        {
            IPAddress ip = GetAddressInternal();

            if (ip != null)
            {
                return await CallbackAsyncInternal(ip, callback, ct).ConfigureAwait(false);
            }

            return false;
        }

        /// <summary>
        /// Tests to see if this object is a Loopback address.
        /// </summary>
        /// <returns>True if it is.</returns>
#pragma warning disable SA1202 // Elements should be ordered by access
        public virtual bool IsLoopback()
#pragma warning restore SA1202 // Elements should be ordered by access
        {
            return IsLoopback(GetAddressInternal());
        }

        /// <summary>
        /// Tests to see if this object is an ip 6 address.
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsIP6()
        {
            return IsIP6(GetAddressInternal());
        }

        /// <summary>
        /// Returns true if this IP address is in the RFC private address range.
        /// </summary>
        /// <returns>True this object has a private address.</returns>
        public virtual bool IsPrivateAddressRange()
        {
            return IsPrivateAddressRange(GetAddressInternal());
        }

        /// <summary>
        /// Tests to see if this object is an AIPIPA address. (169.254.x.x).
        /// </summary>
        /// <returns>True if it is.</returns>
        public virtual bool IsAIPIPA()
        {
            return IsAIPIPA(GetAddressInternal());
        }

        /// <summary>
        /// Copys the contents of another object.
        /// </summary>
        /// <param name="ip">Object to copy.</param>
        public abstract void Copy(IPObject ip);

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public abstract bool Equals(IPObject ip);

        /// <summary>
        /// Compares this to the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object to compare to.</param>
        /// <returns>Equality result.</returns>
        public virtual bool Equals(IPAddress ip)
        {
            return Exists(ip);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPObject ip)
        {
            return Equals(ip);
        }

        /// <summary>
        /// Compares the address in this object and the address in the object passed as a parameter.
        /// </summary>
        /// <param name="ip">Object's IP address to compare to.</param>
        /// <returns>Comparison result.</returns>
        public virtual bool Contains(IPAddress ip)
        {
            return this.Equals(ip);
        }

        /// <summary>
        /// Returns true if IP exists in this parameter.
        /// </summary>
        /// <param name="ip">Address to check for.</param>
        /// <returns>Existential result.</returns>
        public abstract bool Exists(IPAddress ip);

        /// <summary>
        /// Returns true if IP exists in this parameter.
        /// </summary>
        /// <param name="ip">Address to check for.</param>
        /// <returns>Existential result.</returns>
        public virtual bool Exists(IPObject ip)
        {
            return this.Equals(ip);
        }

        /// <summary>
        /// Returns true if IP exists in this parameter.
        /// </summary>
        /// <param name="ip">Address to check for.</param>
        /// <returns>Existential result.</returns>
        public virtual bool Exists(string ip)
        {
            return this.Equals(ip);
        }

        /// <summary>
        /// Returns the address item of the ancestor objects to use in low level functons.
        /// </summary>
        /// <returns>IP address.</returns>
        protected abstract IPAddress GetAddressInternal();

        /// <summary>
        /// Task that pings an IP address.
        /// </summary>
        /// <param name="ip">Host name to ping.</param>
        /// <returns>The result of the ping.</returns>
        protected static async Task<PingReply> PingAsyncInternal(IPAddress ip)
        {
            if (ip != null)
            {
#pragma warning disable IDE0063 // By putting Ping in a using, it ensures that it is disposed off immediately after use.
                using (Ping sender = new Ping())
#pragma warning restore IDE0063
                {
                    PingOptions options = new PingOptions
                    {
                        DontFragment = true,
                    };

                    string data = "JellyFin Ping Request.!!!!!!!!!!";
                    byte[] buffer = Encoding.ASCII.GetBytes(data);

                    return await sender.SendPingAsync(ip, 120, buffer, options).ConfigureAwait(false);
                }
            }

            return null;
        }

        /// <summary>
        /// Sums the value of the byte array.
        /// </summary>
        /// <param name="b">Array to sum.</param>
        /// <param name="start">Starting index.</param>
        /// <param name="finish">Ending index.</param>
        /// <returns>Sum of all the values.</returns>
        protected static int BitSum(byte[] b, int start, int finish)
        {
            int sum = 0;
            for (int i = start; i <= finish; i++)
            {
                sum += b[i];
            }

            return sum;
        }

        /// <summary>
        /// Compares two byte arrays.
        /// </summary>
        /// <param name="src">Array one.</param>
        /// <param name="dest">Array two.</param>
        /// <param name="len">Length of both arrays. Must be the same.</param>
        /// <returns>True if the two arrays match.</returns>
        protected static bool CompareByteArray(byte[] src, byte[] dest, byte len)
        {
            for (int i = 0; i < len; i++)
            {
                if (src[i] != dest[i])
                {
                    return false;
                }
            }

            return true;
        }
    }
}
