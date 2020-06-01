using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace Common.Networking
{
    /// <summary>
    /// A class that holds a list of Network Address objects. (IPAddress, IPNetAddress and IPHostEntry).
    /// </summary>
    public class NetCollection : IEnumerable<IPObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NetCollection"/> class.
        /// </summary>
        /// <param name="item">Object to add.</param>
        public NetCollection(IPObject item)
        {
            Items = new List<IPObject>();
            Add(item);
        }

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
        /// Gets the number in this list.
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets direct access to the list.
        /// </summary>
        public List<IPObject> Items { get; }

        /// <summary>
        /// Parses an array of strings into a NetCollection.
        /// </summary>
        /// <param name="values">Values to parse.</param>
        /// <returns>NetCollection object containing the value strings.</returns>
        public static NetCollection CreateIPCollection(string[] values)
        {
            NetCollection col = new NetCollection();
            if (values != null)
            {
                for (int a = 0; a < values.Length; a++)
                {
                    if (TryParse(values[a], out IPObject item))
                    {
                        col.Add(item);
                    }
                }
            }

            return col;
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

            result = null;
            return false;
        }

        /// <summary>
        /// Assigns the result of a LINQ to this object.
        /// </summary>
        /// <param name="item">LINQ item to assign.</param>
        /// <returns>The object.</returns>
        public NetCollection Assign(IEnumerable<IPObject> item)
        {
            Items.Clear();
            if (item != null)
            {
                Items.AddRange(item.ToList<IPObject>());
            }

            return this;
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
            }
        }

        /// <summary>
        /// Adds a network to the list.
        /// </summary>
        /// <param name="network">Item to add.</param>
        public void Add(IPObject network)
        {
            if (!Exists(network))
            {
                Items.Add(network);
            }
        }

        /// <summary>
        /// Adds a network item to the list.
        /// </summary>
        /// <param name="item">Item to parse.</param>
        public void Add(string item)
        {
            if (!TryParse(item, out IPObject ip))
            {
                throw new ArgumentException("Unable to identify network object.");
            }

            if (!Exists(ip))
            {
                Items.Add(ip);
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
        /// Remove an item from this list.
        /// </summary>
        /// <param name="item">Item to remove.</param>
        /// <returns>True if the item was removed.</returns>
        public bool Remove(IPObject item)
        {
            if (item != null)
            {
                foreach (IPObject i in Items)
                {
                    if (i.Equals(item))
                    {
                        Items.Remove(i);
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Excludes all the items from this list that are found in excludeList.
        /// </summary>
        /// <param name="excludeList">Items to exlude.</param>
        /// <returns>A new collection.</returns>
        public NetCollection Exclude(NetCollection excludeList)
        {
            NetCollection results = new NetCollection();

            if (excludeList != null)
            {
                bool found;
                foreach (var outer in Items)
                {
                    found = false;

                    foreach (var inner in excludeList.Items)
                    {
                        if (inner.Equals(outer))
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
            }

            return results;
        }

        /// <summary>
        /// Returns true is the item contains an item with the ip address, or the ip address falls within any of the networks.
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Contains(IPAddress search)
        {
            if (search != null)
            {
                foreach (var item in Items)
                {
                    if (item.Contains(search))
                    {
                        return true;
                    }
                }
            }

            return false;
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
        /// Returns true is the item contains an item with the ip address, or the ip address falls within any of the networks.
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Contains(string search)
        {
            if (IPAddress.TryParse(search, out IPAddress a))
            {
                foreach (var item in Items)
                {
                    if (item.Contains(a))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true is the item contains an item with the ip address, or the ip address falls within any of the networks.
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Contains(IPObject search)
        {
            if (search != null)
            {
                foreach (var item in Items)
                {
                    if (item.Contains(search))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true is the item contains an item. Matches networks, ip address and host names.
        /// </summary>
        /// <param name="search">The item to look for.</param>
        /// <returns>True or false.</returns>
        public bool Equals(IPAddress search)
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
        /// Returns true is the collection contains the ip address.
        /// </summary>
        /// <param name="item">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public bool Equals(string item)
        {
            if (!string.IsNullOrEmpty(item) && TryParse(item, out IPObject networkItem))
            {
                return Equals(networkItem);
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
            if (networkItem != null)
            {
                foreach (IPObject i in Items)
                {
                    if (i.Exists(networkItem))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Returns true is the collection contains the ip object.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public bool Exists(IPObject networkItem)
        {
            if (networkItem != null)
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
        /// Locates and returns the object that matches the IP Address.
        /// </summary>
        /// <param name="networkItem">IP address to search for.</param>
        /// <returns>True of false.</returns>
        public IPObject Find(IPAddress networkItem)
        {
            if (networkItem != null)
            {
                foreach (IPObject i in Items)
                {
                    if (i.Equals(networkItem))
                    {
                        return i;
                    }
                }
            }

            return null;
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
    }
}
