using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Numerics;

namespace Jellyfin.Server.Implementations.Networking.IPNetwork
{
    public class IPAddressCollection : IEnumerable<IPAddress>, IEnumerator<IPAddress>
    {

        private IPNetwork _ipnetwork;
        private BigInteger _enumerator;

        internal IPAddressCollection(IPNetwork ipnetwork)
        {
            this._ipnetwork = ipnetwork;
            this._enumerator = -1;
        }


        #region Count, Array, Enumerator

        public BigInteger Count => this._ipnetwork.Total;

        public IPAddress this[BigInteger i]
        {
            get
            {
                if (i >= this.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(i));
                }
                byte width = this._ipnetwork.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? (byte)32 : (byte)128;
                var ipn = this._ipnetwork.Subnet(width);
                return ipn[i].Network;
            }
        }

        #endregion

        #region IEnumerable Members

        IEnumerator<IPAddress> IEnumerable<IPAddress>.GetEnumerator()
        {
            return this;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this;
        }

        #region IEnumerator<IPNetwork> Members

        public IPAddress Current => this[this._enumerator];

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            // nothing to dispose
            return;
        }

        #endregion

        #region IEnumerator Members

        object IEnumerator.Current => this.Current;

        public bool MoveNext()
        {
            this._enumerator++;
            if (this._enumerator >= this.Count)
            {
                return false;
            }
            return true;

        }

        public void Reset()
        {
            this._enumerator = -1;
        }

        #endregion

        #endregion
    }
}
