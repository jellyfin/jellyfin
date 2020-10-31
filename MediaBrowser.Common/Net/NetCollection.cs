using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// A class that holds a list of Network Address objects. (IPAddress, IPNetAddress and IPHostEntry).
    /// </summary>
    public class NetCollection : Collection<IPObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="item">Items to assign.</param>
        public NetCollection(IEnumerable<IPObject> item)
            : base()
        {
            ((List<IPObject>)Items).AddRange(item.ToList<IPObject>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="net">NetCollection class to copy initial values from.</param>
        public NetCollection(NetCollection net)
            : base()
        {
            if (net != null)
            {
                ((List<IPObject>)Items).AddRange(net.Items);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        public NetCollection()
            : base()
        {
        }

        /// <summary>
        /// Compares two NetCollection objects. The order is ignored.
        /// </summary>
        /// <param name="dest">Item to compare to.</param>
        /// <returns>True if both are equal.</returns>
        public bool Equals(NetCollection dest)
        {
            if (dest == null || Count != dest.Count)
            {
                return false;
            }

            foreach (var sourceItem in Items)
            {
                bool found = false;
                foreach (var destItem in dest)
                {
                    if (sourceItem.Equals(destItem))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Add an address to the list.
        /// </summary>
        /// <param name="ip">Item to add.</param>
        public void Add(IPAddress ip)
        {
            if (!NetworkExtensions.ContainsAddress(this, ip))
            {
                base.Add(new IPNetAddress(ip, 32));
            }
        }

        /// <summary>
        /// Adds a network to the list.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public new void Add(IPObject item)
        {
            if (!NetworkExtensions.ContainsAddress(this, item))
            {
                base.Add(item);
            }
        }

        /// <summary>
        /// Converts this object to a string.
        /// </summary>
        /// <returns>Returns a string representation of this object.</returns>
        public override string ToString()
        {
            string output = "[";
            if (Count > 0)
            {
                foreach (var i in Items)
                {
                    output += $"{i},";
                }

                output = output[0..^1];
            }

            return $"{output}]";
        }
    }
}
