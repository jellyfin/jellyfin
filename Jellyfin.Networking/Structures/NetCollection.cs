using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Jellyfin.Networking.Structures
{
    /// <summary>
    /// A class that holds a list of Network Address objects. (IPAddress, IPNetAddress and IPHostEntry).
    /// </summary>
    public sealed class NetCollection : ICollection<IPObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="item">Items to assign.</param>
        public NetCollection(IEnumerable<IPObject> item)
        {
            Items = new List<IPObject>();
            Items.AddRange(item.ToList<IPObject>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="net">NetCollection class to copy initial values from.</param>
        public NetCollection(NetCollection net)
        {
            Items = new List<IPObject>();

            if (net != null)
            {
                Items.AddRange(net.Items);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="unique">There will be no duplicate items in this collection.</param>
        public NetCollection(bool unique = true)
        {
            Items = new List<IPObject>();
            Unique = unique;
        }

        /// <summary>
        /// Gets a value indicating whether this collection contains unique items.
        /// </summary>
        public bool Unique { get; }

        /// <summary>
        /// Gets the number in this list.
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets a value indicating whether this collection is readonly.
        /// </summary>
        public bool IsReadOnly => false;

        /// <summary>
        /// Gets direct access to the list.
        /// </summary>
        private List<IPObject> Items { get; }

        /// <summary>
        /// Collection index.
        /// </summary>
        /// <param name="index">The index offset.</param>
        /// <returns>The item at the position in the collection.</returns>
        public IPObject this[int index]
        {
            get
            {
                return (IPObject)Items[index];
            }

            set
            {
                Items[index] = value;
            }
        }

        /// <summary>
        /// Trys to identify the string and return an object of that class.
        /// </summary>
        /// <param name="addr">String to parse.</param>
        /// <param name="result">IPObject to return.</param>
        /// <returns>True if the value parsed successfully.</returns>
        public static bool TryParse(string addr, out IPObject result)
        {
            if (!string.IsNullOrEmpty(addr))
            {
                // Is it an IP address
                if (IPNetAddress.TryParse(addr, out IPNetAddress nw))
                {
                    result = nw;
                    return true;
                }

                if (IPHost.TryParse(addr, out IPHost h))
                {
                    result = h;
                    return true;
                }
            }

            result = IPNetAddress.None;
            return false;
        }

        /// <summary>
        /// Returns a collection containing the subnets of this collection given.
        /// </summary>
        /// <param name="nc">NetCollection to process.</param>
        /// <returns>NetCollection object containing the subnets.</returns>
        public static NetCollection AsNetworks(NetCollection nc)
        {
            if (nc == null)
            {
                throw new ArgumentNullException(nameof(nc));
            }

            NetCollection res = new NetCollection();

            foreach (IPObject i in nc)
            {
                if (i is IPNetAddress nw)
                {
                    // Add the subnet calculated from the interface address/mask.
                    IPNetAddress lan = new IPNetAddress(nw.NetworkAddress.Address, nw.NetworkAddress.PrefixLength)
                    {
                        Tag = i.Tag
                    };

                    res.Add(lan);
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
        /// Add an address to the list.
        /// </summary>
        /// <param name="ip">Item to add.</param>
        public void Add(IPAddress ip)
        {
            if (!Unique || !Contains(ip))
            {
                Items.Add(new IPNetAddress(ip, 32));
            }
        }

        /// <summary>
        /// Adds a network to the list.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Add(IPObject item)
        {
            if (!Unique || !Contains(item))
            {
                Items.Add(item);
            }
        }

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        public void Clear()
        {
            Items.Clear();
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="excludeList">Items to exclude.</param>
        /// <returns>A new collection, with the items excluded.</returns>
        public NetCollection Exclude(NetCollection excludeList)
        {
            if (Count == 0 || excludeList == null)
            {
                return new NetCollection(this);
            }

            NetCollection results = new NetCollection();

            bool found;
            foreach (var outer in Items)
            {
                found = false;

                foreach (var inner in excludeList.Items)
                {
                    if (outer.Equals(inner))
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    results.Items.Add(outer);
                }
            }

            return results;
        }

        /// <summary>
        /// Remove an item from this list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(IPObject item)
        {
            if (Count == 0)
            {
                return false;
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            foreach (IPObject i in Items)
            {
                if (i.Equals(item))
                {
                    Items.Remove(i);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <param name="match">The item that contains the item specified.</param>
        /// <returns>True if the collection contains the item.</returns>
        public bool Contains(IPObject item, out IPObject? match)
        {
            if (Count == 0)
            {
                match = null;
                return false;
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            foreach (var i in Items)
            {
                if (i.AddressFamily == item.AddressFamily && i.Contains(item))
                {
                    match = i;
                    return true;
                }
            }

            match = null;
            return false;
        }

        /// <summary>
        /// Returns true if the collection contains an item with the ip address,
        /// or the ip address falls within any of the collection's network ranges.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public bool Contains(IPObject item)
        {
            if (Count == 0)
            {
                return false;
            }

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            foreach (var i in Items)
            {
                if (i.AddressFamily == item.AddressFamily && i.Contains(item))
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
        /// <param name="item">The item to look for.</param>
        /// <returns>True if the collection contains the item.</returns>
        public bool Contains(IPAddress item)
        {
            if (Count == 0)
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

            foreach (var i in Items)
            {
                if (i.AddressFamily == item.AddressFamily && i.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true if the item contains an item. (Matches networks, ip address and host names).
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True if the item exists in the collection.</returns>
        public bool Equals(IPObject search)
        {
            if (Count == 0)
            {
                return false;
            }

            if (search != null)
            {
                foreach (var item in Items)
                {
                    if (item.Equals(search))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns all items that co-exist in this object and target.
        /// </summary>
        /// <param name="target">Collection to compare with.</param>
        /// <returns>A collection containing all the matches.</returns>
        public NetCollection Union(NetCollection target)
        {
            if (Count == 0)
            {
                return new NetCollection();
            }

            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            NetCollection nc = new NetCollection();

            foreach (IPObject i in target.Items)
            {
                if (Contains(i, out IPObject? match))
                {
                    if (match != null) // && IPObject.MaskToCidr(match.Mask) < IPObject.MaskToCidr(i.Mask))
                    {
                        nc.Add(match);
                    }
                    else
                    {
                        nc.Add(i);
                    }
                }
            }

            return nc;
        }

        /// <summary>
        /// Returns true is the collection contains the ip object.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True if the item exists in the collection.</returns>
        public bool Exists(IPObject networkItem)
        {
            if (Count == 0)
            {
                return false;
            }

            if (networkItem == null)
            {
                throw new ArgumentNullException(nameof(networkItem));
            }

            foreach (IPObject i in Items)
            {
                if (i.Equals(networkItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true is the collection contains the ip object.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True if the item exists in the collection.</returns>
        public bool Exists(string networkItem)
        {
            if (Count == 0)
            {
                return false;
            }

            if (!string.IsNullOrEmpty(networkItem))
            {
                foreach (IPObject i in Items)
                {
                    if (i.Equals(networkItem))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Compares two NetCollection objects. Order is ignored.
        /// </summary>
        /// <param name="dest">Item to compare to.</param>
        /// <returns>True if both are equal.</returns>
        public bool Equals(NetCollection dest)
        {
            if (dest == null || Count != dest.Count)
            {
                return false;
            }

            foreach (var item in Items)
            {
                if (!dest.Exists(item))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Returns true is the collection contains the ip address.
        /// </summary>
        /// <param name="item">IP address to search for.</param>
        /// <returns>True if the item exists in the collection.</returns>
        public bool Exists(IPAddress item)
        {
            if (Count == 0)
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

            foreach (IPObject i in Items)
            {
                if (i.Exists(item))
                {
                    return true;
                }
            }

            return false;
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

                output = output[0..^1]; // output = output.Remove(output.Length - 1);
            }

            return $"{output}]";
        }

        /// <summary>
        /// Enumerator function.
        /// </summary>
        /// <returns>The IEnumerator function.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Enumerator function.
        /// </summary>
        /// <returns>The IEnumerator{IPObject} function.</returns>
        public IEnumerator<IPObject> GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// Copies the entire Collection to a compatible one-dimensional Array, starting at the specified index of the target array.
        /// </summary>
        /// <param name="array">The one-dimensional Array that is the destination of the elements copied from Collection. The Array must have zero-based indexing.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        public void CopyTo(IPObject[] array, int arrayIndex)
        {
            Items.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Callback function that returns the first n IP addresses of the callback that succeeds.
        /// </summary>
        /// <param name="callback">Delegate function to call for each ip.</param>
        /// <param name="cancellationToken">Cancellation Token.</param>
        /// <param name="n">Limits the response to the first n items.</param>
        /// <returns>NetCollection object.</returns>
        public NetCollection Callback(
            Func<IPObject, CancellationToken, Task<bool>> callback,
            CancellationToken cancellationToken,
            int n = -1)
        {
            NetCollection interfaces = new NetCollection(this);
            if (Count == 0)
            {
                return interfaces;
            }

            IEnumerable<Task<bool>> tasks = from ip in interfaces select callback(ip, cancellationToken);
            if (n == -1)
            {
                n = Count + 1; // Return all.
            }

            NetCollection res = new NetCollection();
            var taskList = tasks.ToList<Task>();
            while (taskList.Count > 0)
            {
                int taskIndex = Task.WaitAny(tasks.ToArray<Task>());
                if (cancellationToken.IsCancellationRequested)
                {
                    return res;
                }

                bool success = ((Task<bool>)taskList[taskIndex]).Result;
                if (success)
                {
                    res.Add(interfaces.Items[taskIndex]);
                    n--;
                    if (n <= 0)
                    {
                        break;
                    }
                }

                taskList.RemoveAt(taskIndex);
                interfaces.Items.RemoveAt(taskIndex);
            }

            return res;
        }
    }
}
