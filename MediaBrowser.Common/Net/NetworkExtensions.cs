using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;

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
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="ip">Item to add.</param>
        public static void AddItem(this Collection<IPNetAddress> source, IPAddress ip)
        {
            if (!source.ContainsAddress(ip))
            {
                source.Add(new IPNetAddress(ip, 32));
            }
        }

        /// <summary>
        /// Adds a network to the collection.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="item">Item to add.</param>
        public static void AddItem(this Collection<IPNetAddress> source, IPNetAddress item)
        {
            if (!source.ContainsAddress(item))
            {
                source.Add(item);
            }
        }

        /// <summary>
        /// Converts this object to a string.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{IPNetAddress}"/>.</param>
        /// <returns>Returns a string representation of this object.</returns>
        public static string AsString(this IEnumerable<IPNetAddress> source)
        {
            return $"[{string.Join(',', source)}]";
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{IPNetAddress}"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this IEnumerable<IPNetAddress> source, IPAddress item)
        {
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
        /// <param name="source">The <see cref="IEnumerable{IPNetAddress}"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this IEnumerable<IPNetAddress> source, IPNetAddress item)
        {
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
        /// Compares two Collection{IPNetAddress} objects. The order is ignored.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="dest">Item to compare to.</param>
        /// <returns>True if both are equal.</returns>
        public static bool Compare(this Collection<IPNetAddress> source, Collection<IPNetAddress> dest)
        {
            if (dest == null || source.Count != dest.Count)
            {
                return false;
            }

            foreach (var sourceItem in source)
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
        /// Returns a collection containing the subnets of this collection given.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <returns>Collection{IPNetAddress} object containing the subnets.</returns>
        public static Collection<IPNetAddress> AsNetworkAddresses(this Collection<IPNetAddress> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            Collection<IPNetAddress> res = new Collection<IPNetAddress>();

            foreach (IPNetAddress i in source)
            {
                if (i is IPHost ipHost)
                {
                    // Flatten out IPHost and add all its ip addresses.
                    foreach (var addr in ipHost.GetAddresses())
                    {
                        IPNetAddress host = new IPNetAddress(addr)
                        {
                            Tag = i.Tag
                        };

                        res.AddItem(host);
                    }
                }
                else
                {
                    // Add the subnet calculated from the interface address/mask.
                    res.AddItem(i.NetworkAddress);
                }
            }

            return res;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{IPNetAddress}"/>.</param>
        /// <param name="excludeList">Items to exclude.</param>
        /// <returns>A new collection, with the items excluded.</returns>
        public static Collection<IPNetAddress> Exclude(this IList<IPNetAddress> source, IEnumerable<IPNetAddress> excludeList)
        {
            if (excludeList == null)
            {
                return new Collection<IPNetAddress>(source);
            }

            Collection<IPNetAddress> results = new Collection<IPNetAddress>();

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
                    results.AddItem(outer);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all items that co-exist in this object and target.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="target">Collection to compare with.</param>
        /// <returns>A collection containing all the matches.</returns>
        public static Collection<IPNetAddress> Union(this Collection<IPNetAddress> source, Collection<IPNetAddress> target)
        {
            if (source.Count == 0)
            {
                return new Collection<IPNetAddress>();
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            Collection<IPNetAddress> nc = new Collection<IPNetAddress>();

            foreach (IPNetAddress i in source)
            {
                if (target.ContainsAddress(i))
                {
                    nc.AddItem(i);
                }
            }

            return nc;
        }
    }
}
