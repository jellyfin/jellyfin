using System;
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
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="ip">Item to add.</param>
        public static void AddItem(this Collection<IPObject> source, IPAddress ip)
        {
            if (!source.ContainsAddress(ip))
            {
                source.Add(new IPNetAddress(ip, 32));
            }
        }

        /// <summary>
        /// Adds a network to the collection.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="item">Item to add.</param>
        /// <param name="itemsAreNetworks">If <c>true</c> the values are treated as subnets.
        /// If <b>false</b> items are addresses.</param>
        public static void AddItem(this Collection<IPObject> source, IPObject item, bool itemsAreNetworks = true)
        {
            if (!source.ContainsAddress(item) || !itemsAreNetworks)
            {
                source.Add(item);
            }
        }

        /// <summary>
        /// Converts this object to a string.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <returns>Returns a string representation of this object.</returns>
        public static string AsString(this Collection<IPObject> source)
        {
            return $"[{string.Join(',', source)}]";
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this Collection<IPObject> source, IPAddress item)
        {
            if (source.Count == 0)
            {
                return false;
            }

            ArgumentNullException.ThrowIfNull(item);

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
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public static bool ContainsAddress(this Collection<IPObject> source, IPObject item)
        {
            if (source.Count == 0)
            {
                return false;
            }

            ArgumentNullException.ThrowIfNull(item);

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
        /// Compares two Collection{IPObject} objects. The order is ignored.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="dest">Item to compare to.</param>
        /// <returns>True if both are equal.</returns>
        public static bool Compare(this Collection<IPObject> source, Collection<IPObject> dest)
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
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <returns>Collection{IPObject} object containing the subnets.</returns>
        public static Collection<IPObject> AsNetworks(this Collection<IPObject> source)
        {
            ArgumentNullException.ThrowIfNull(source);

            Collection<IPObject> res = new Collection<IPObject>();

            foreach (IPObject i in source)
            {
                if (i is IPNetAddress nw)
                {
                    // Add the subnet calculated from the interface address/mask.
                    var na = nw.NetworkAddress;
                    na.Tag = i.Tag;
                    res.AddItem(na);
                }
                else if (i is IPHost ipHost)
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
            }

            return res;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="excludeList">Items to exclude.</param>
        /// <param name="isNetwork">Collection is a network collection.</param>
        /// <returns>A new collection, with the items excluded.</returns>
        public static Collection<IPObject> Exclude(this Collection<IPObject> source, Collection<IPObject> excludeList, bool isNetwork)
        {
            if (source.Count == 0 || excludeList == null)
            {
                return new Collection<IPObject>(source);
            }

            Collection<IPObject> results = new Collection<IPObject>();

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
                    results.AddItem(outer, isNetwork);
                }
            }

            return results;
        }

        /// <summary>
        /// Returns all items that co-exist in this object and target.
        /// </summary>
        /// <param name="source">The <see cref="Collection{IPObject}"/>.</param>
        /// <param name="target">Collection to compare with.</param>
        /// <returns>A collection containing all the matches.</returns>
        public static Collection<IPObject> ThatAreContainedInNetworks(this Collection<IPObject> source, Collection<IPObject> target)
        {
            if (source.Count == 0)
            {
                return new Collection<IPObject>();
            }

            ArgumentNullException.ThrowIfNull(target);

            Collection<IPObject> nc = new Collection<IPObject>();

            foreach (IPObject i in source)
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
