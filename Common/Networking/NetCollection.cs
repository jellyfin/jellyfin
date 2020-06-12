namespace Common.Networking
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// A class that holds a list of Network Address objects. (IPAddress, IPNetAddress and IPHostEntry).
    /// </summary>
    public sealed class NetCollection : ICollection<IPObject>
    {
        /// <summary>
        /// Optimization flag.
        /// Don't recalculate network addresses of items as this collection only contains network addresses.
        /// </summary>
        private bool _network;

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
        public NetCollection()
        {
            Items = new List<IPObject>();
        }

        /// <summary>
        /// Gets the number in this list..
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets the Items
        /// Gets direct access to the list..
        /// </summary>
        public List<IPObject> Items { get; }

        /// <summary>
        /// Gets a value indicating whether this collection is readonly..
        /// </summary>
        public bool IsReadOnly => false;

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

            result = null;
            return false;
        }

        /// <summary>
        /// Returns all the subnets of a NetCollection.
        /// </summary>
        /// <param name="nc">NetCollection to convert.</param>
        /// <returns>NetCollection object contains subnets.</returns>
        public static NetCollection AsNetworks(NetCollection nc)
        {
            if (nc == null)
            {
                throw new ArgumentException("Parameter cannot be null.");
            }

            NetCollection res = new NetCollection();

            foreach (IPObject i in nc)
            {
                if (i is IPNetAddress nw)
                {
                    if (!nw.IsLoopback())
                    {
                        // Add the subnet calculated from the interface address/mask.
                        IPNetAddress lan = new IPNetAddress(IPObject.NetworkAddress(nw.Address, nw.Mask), nw.Mask)
                        {
                            Tag = i.Tag
                        };
                        res.Add(lan);
                    }
                }
                else
                {
                    // Flatten out IPHost and add all its ip addresses.
                    foreach (var addr in ((IPHost)i).Addresses)
                    {
                        if (!IPObject.IsLoopback(addr))
                        {
                            IPNetAddress host = new IPNetAddress(addr, 32)
                            {
                                Tag = i.Tag
                            };
                            res.Add(host);
                        }
                    }
                }
            }

            res._network = true;
            return res;
        }

        /// <summary>
        /// Add an address to the list.
        /// </summary>
        /// <param name="ip">Item to add.</param>
        public void Add(IPAddress ip)
        {
            if (!Exists(ip))
            {
                Items.Add(new IPNetAddress(ip, 32));
                _network = false;
            }
        }

        /// <summary>
        /// Adds a network to the list.
        /// </summary>
        /// <param name="item">Item to add.</param>
        public void Add(IPObject item)
        {
            if (!Exists(item))
            {
                Items.Add(item);
                _network = false;
            }
        }

        /// <summary>
        /// Removes all items from the list.
        /// </summary>
        public void Clear()
        {
            Items.Clear();
            _network = false;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="excludeList">Items to exlude.</param>
        /// <returns>A new collection.</returns>
        public NetCollection Exclude(NetCollection excludeList)
        {
            NetCollection results = new NetCollection();

            if (excludeList == null)
            {
                throw new ArgumentException("Argument cannot be null.");
            }

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

            results._network = excludeList._network;
            return results;
        }

        /// <summary>
        /// Returns true if this object has any values.
        /// </summary>
        /// <returns>True if Count > 0.</returns>
        public bool Any()
        {
            return Count > 0;
        }

        /// <summary>
        /// Remove an item from this list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(IPObject item)
        {
            if (item == null)
            {
                throw new ArgumentException("Argument cannot be null.");
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
        /// Returns true is the item contains an item with the ip address, or the ip address falls within any of the networks.
        /// </summary>
        /// <param name="item">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Contains(IPObject item)
        {
            if (item == null)
            {
                throw new ArgumentException("Argument cannot be null.");
            }

            foreach (var i in Items)
            {
                if (i.AddressFamily == item.AddressFamily
                    && i.Contains(item))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true is the item contains an item. Matches networks, ip address and host names.
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Equals(IPObject search)
        {
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
        /// <param name="target">NetCollection to compare with.</param>
        /// <returns>A NetCollection containing all the matches.</returns>
        public NetCollection Union(NetCollection target)
        {
            if (target == null)
            {
                throw new ArgumentException("Argument cannot be null.");
            }

            NetCollection nc = new NetCollection();

            foreach (IPObject i in target)
            {
                if (Equals(i))
                {
                    nc.Add(i);
                }
            }

            return nc;
        }

        /// <summary>
        /// Returns true is the collection contains the ip object.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public bool Exists(IPObject networkItem)
        {
            if (networkItem == null)
            {
                throw new ArgumentException("Argument cannot be null.");
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
        /// <returns>True of false.</returns>
        public bool Exists(string networkItem)
        {
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
        /// Returns true is the collection contains the ip address.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public bool Exists(IPAddress networkItem)
        {
            if (networkItem == null)
            {
                throw new ArgumentException("Argument cannot be null.");
            }

            foreach (IPObject i in Items)
            {
                if (i.Exists(networkItem))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Locates and returns the object that matches the IP Address.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public IPObject Find(IPAddress networkItem)
        {
            if (networkItem == null)
            {
                throw new ArgumentException("Argument cannot be null.");
            }

            foreach (IPObject i in Items)
            {
                if (i.Equals(networkItem))
                {
                    return i;
                }
            }

            return null;
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

                // output = output[0..^1];
                output = output.Remove(output.Length - 1);
            }

            return $"{output}]";
        }

        /// <summary>
        /// Enumerator function.
        /// </summary>
        /// <returns>The enumerator function.</returns>
        public IEnumerator GetEnumerator()
        {
            return Items.GetEnumerator();
        }

        /// <summary>
        /// Enumerator function.
        /// </summary>
        /// <returns>The enumerator function.</returns>
        IEnumerator<IPObject> IEnumerable<IPObject>.GetEnumerator()
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
