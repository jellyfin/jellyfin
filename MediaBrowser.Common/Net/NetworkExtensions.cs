#pragma warning disable CA1062 // Validate arguments of public methods
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using NetCollection = System.Collections.ObjectModel.Collection<MediaBrowser.Common.Net.IPObject>;

namespace MediaBrowser.Common.Net
{
    /// <summary>
    /// Defines the <see cref="NetworkExtensions" />.
    /// </summary>
    public static class NetworkExtensions
    {
        /// <summary>
        /// Add an address to the collection.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="ip">Item to add.</param>
        public static void AddItem(this NetCollection source, IPAddress ip)
        {
            if (!source.ContainsAddress(ip))
            {
                source.Add(new IPNetAddress(ip, 32));
            }
        }

        /// <summary>
        /// Add multiple items to the collection.
        /// </summary>
        /// <param name="destination">The <see cref="NetCollection"/>.</param>
        /// <param name="source">Item to add.</param>
        /// <returns>Return the collection.</returns>
        public static NetCollection AddRange(this NetCollection destination, IEnumerable<IPObject> source)
        {
            foreach (var item in source)
            {
                destination.Add(item);
            }

            return destination;
        }

        /// <summary>
        /// Adds a network to the collection.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="item">Item to add.</param>
        public static void AddItem(this NetCollection source, IPObject item)
        {
            if (!source.ContainsAddress(item))
            {
                source.Add(item);
            }
        }

        /// <summary>
        /// Converts this object to a string.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <returns>Returns a string representation of this object.</returns>
        public static string Readable(this NetCollection source)
        {
            string output = "[";
            if (source.Count > 0)
            {
                foreach (var i in source)
                {
                    output += $"{i},";
                }

                output = output[0..^1];
            }

            return $"{output}]";
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this NetCollection source, IPAddress item)
        {
            if (source.Count == 0)
            {
                return false;
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.IsIPv4MappedToIPv6)
            {
                item = item.MapToIPv4();
            }

            foreach (var i in source)
            {
                if (i.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this NetCollection source, IPObject item)
        {
            if (source.Count == 0)
            {
                return false;
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            foreach (var i in source)
            {
                if (i.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns a collection containing the subnets of this collection given.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <returns>NetCollection object containing the subnets.</returns>
        public static NetCollection AsNetworks(this NetCollection source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            NetCollection res = new NetCollection();

            foreach (IPObject i in source)
            {
                if (i is IPNetAddress nw)
                {
                    // Add the subnet calculated from the interface address/mask.
                    var na = nw.NetworkAddress;
                    na.Tag = i.Tag;
                    res.Add(na);
                }
                else
                {
                    // Flatten out IPHost and add all its ip addresses.
                    foreach (var addr in ((IPHost)i).GetAddresses())
                    {
                        IPNetAddress host = new IPNetAddress(addr)
                        {
                            Tag = i.Tag
                        };

                        res.Add(host);
                    }
                }
            }

            return res;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="excludeList">Items to exclude.</param>
        /// <returns>A new collection, with the items excluded.</returns>
        public static NetCollection Exclude(this NetCollection source, NetCollection excludeList)
        {
            if (source.Count == 0 || excludeList == null)
            {
                return new NetCollection(source);
            }

            NetCollection results = new NetCollection();

            bool found;
            foreach (var outer in source)
            {
                found = false;

                foreach (var inner in excludeList)
                {
                    if (outer.Equals(inner))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    results.Add(outer);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all items that co-exist in this object and target.
        /// </summary>
        /// <param name="source">The <see cref="NetCollection"/>.</param>
        /// <param name="target">Collection to compare with.</param>
        /// <returns>A collection containing all the matches.</returns>
        public static NetCollection Union(this NetCollection source, NetCollection target)
        {
            if (source.Count == 0)
            {
                return new NetCollection();
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            NetCollection nc = new NetCollection();

            foreach (IPObject i in source)
            {
                if (target.ContainsAddress(i))
                {
                    nc.Add(i);
                }
            }

            return nc;
        }
    }
}
