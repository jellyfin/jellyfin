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
        /// Adds a network to the collection.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="item">Item to add.</param>
        /// <param name="itemsAreNetworks">If <c>true</c> the values are treated as subnets.
        /// If <b>false</b> items are addresses.</param>
        public static void AddItem(this Collection<IPNetAddress> source, IPNetAddress item, bool itemsAreNetworks)
        {
            if (!itemsAreNetworks || !source.ContainsAddress(item))
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
        /// Returns a collection containing the subnets of this collection given.
        /// </summary>
        /// <param name="source">The <see cref="IEnumerable{IPNetAddress}"/>.</param>
        /// <returns>Collection{IPNetAddress} object containing the subnets.</returns>
        public static Collection<IPNetAddress> AsNetworkAddresses(this IEnumerable<IPNetAddress> source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var result = new Collection<IPNetAddress>();

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

                        result.AddItem(host, true);
                    }
                }
                else
                {
                    // Add the subnet calculated from the interface address/mask.
                    result.AddItem(i.Network, true);
                }
            }

            return result;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPNetAddress}"/>.</param>
        /// <param name="excludeList">Items to exclude.</param>
        /// <param name="isNetwork">Collection is a network collection.</param>
        /// <returns>A new collection, with the items excluded.</returns>
        public static IEnumerable<IPNetAddress> Exclude(this IEnumerable<IPNetAddress> source, IEnumerable<IPNetAddress> excludeList, bool isNetwork)
        {
            if (excludeList == null)
            {
                return source;
            }

            var results = new Collection<IPNetAddress>();

            foreach (var outer in source)
            {
                bool found = false;

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
                    results.AddItem(outer, isNetwork);
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
        public static Collection<IPNetAddress> ThatAreContainedInNetworks(this Collection<IPNetAddress> source, Collection<IPNetAddress> target)
        {
            if (source.Count == 0)
            {
                return new Collection<IPNetAddress>();
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            var results = new Collection<IPNetAddress>();

            foreach (IPNetAddress i in source)
            {
                if (target.ContainsAddress(i))
                {
                    results.AddItem(i, true);
                }
            }

            return results;
        }
    }
}
