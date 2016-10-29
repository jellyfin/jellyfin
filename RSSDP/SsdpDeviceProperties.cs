using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rssdp
{
	/// <summary>
	/// Represents a collection of <see cref="SsdpDeviceProperty"/> instances keyed by the <see cref="SsdpDeviceProperty.FullName"/> property value.
	/// </summary>
	/// <remarks>
	/// <para>Items added to this collection are keyed by their <see cref="SsdpDeviceProperty.FullName"/> property value, at the time they were added. If the name changes after they were added to the collection, the key is not updated unless the item is manually removed and re-added to the collection.</para>
	/// </remarks>
	public class SsdpDevicePropertiesCollection : IEnumerable<SsdpDeviceProperty>
	{

		#region Fields

		private IDictionary<string, SsdpDeviceProperty> _Properties;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SsdpDevicePropertiesCollection()
		{
			_Properties = new Dictionary<string, SsdpDeviceProperty>();
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="capacity">Specifies the initial capacity of the collection.</param>
		public SsdpDevicePropertiesCollection(int capacity)
		{
			_Properties = new Dictionary<string, SsdpDeviceProperty>(capacity);
		}

		#endregion

		#region Public Methpds

		/// <summary>
		/// Adds a <see cref="SsdpDeviceProperty"/> instance to the collection.
		/// </summary>
		/// <param name="customDeviceProperty">The property instance to add to the collection.</param>
		/// <remarks>
		/// <para></para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="customDeviceProperty"/> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="SsdpDeviceProperty.FullName"/> property of the <paramref name="customDeviceProperty"/> argument  is null or empty string, or if the collection already contains an item with the same key.</exception>
		public void Add(SsdpDeviceProperty customDeviceProperty)
		{
			if (customDeviceProperty == null) throw new ArgumentNullException("customDeviceProperty");
			if (String.IsNullOrEmpty(customDeviceProperty.FullName)) throw new ArgumentException("customDeviceProperty.FullName cannot be null or empty.");

			lock (_Properties)
			{
				_Properties.Add(customDeviceProperty.FullName, customDeviceProperty);
			}
		}

		#region Remove Overloads

		/// <summary>
		/// Removes the specified property instance from the collection.
		/// </summary>
		/// <param name="customDeviceProperty">The <see cref="SsdpDeviceProperty"/> instance to remove from the collection.</param>
		/// <remarks>
		/// <para>Only remove the specified property if that instance was in the collection, if another property with the same full name exists in the collection it is not removed.</para>
		/// </remarks>
		/// <returns>True if an item was removed from the collection, otherwise false (because it did not exist or was not the same instance).</returns>
		/// <seealso cref="Remove(string)"/>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="customDeviceProperty"/> is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <see cref="SsdpDeviceProperty.FullName"/> property of the <paramref name="customDeviceProperty"/> argument  is null or empty string, or if the collection already contains an item with the same key.</exception>
		public bool Remove(SsdpDeviceProperty customDeviceProperty)
		{
			if (customDeviceProperty == null) throw new ArgumentNullException("customDeviceProperty");
			if (String.IsNullOrEmpty(customDeviceProperty.FullName)) throw new ArgumentException("customDeviceProperty.FullName cannot be null or empty.");

			lock (_Properties)
			{
				if (_Properties.ContainsKey(customDeviceProperty.FullName) && _Properties[customDeviceProperty.FullName] == customDeviceProperty)
					return _Properties.Remove(customDeviceProperty.FullName);
			}

			return false;
		}

		/// <summary>
		/// Removes the property with the specified key (<see cref="SsdpDeviceProperty.FullName"/> from the collection.
		/// </summary>
		/// <param name="customDevicePropertyFullName">The full name of the <see cref="SsdpDeviceProperty"/> instance to remove from the collection.</param>
		/// <returns>True if an item was removed from the collection, otherwise false (because no item exists in the collection with that key).</returns> 
		/// <exception cref="System.ArgumentException">Thrown if the <paramref name="customDevicePropertyFullName"/> argument is null or empty string.</exception>
		public bool Remove(string customDevicePropertyFullName)
		{
			if (String.IsNullOrEmpty(customDevicePropertyFullName)) throw new ArgumentException("customDevicePropertyFullName cannot be null or empty.");

			lock (_Properties)
			{
				return _Properties.Remove(customDevicePropertyFullName);
			}
		}

		#endregion

		/// <summary>
		/// Returns a boolean indicating whether or not the specified <see cref="SsdpDeviceProperty"/> instance is in the collection.
		/// </summary>
		/// <param name="customDeviceProperty">An <see cref="SsdpDeviceProperty"/> instance to check the collection for.</param>
		/// <returns>True if the specified instance exists in the collection, otherwise false.</returns>
		public bool Contains(SsdpDeviceProperty customDeviceProperty)
		{
			if (customDeviceProperty == null) throw new ArgumentNullException("customDeviceProperty");
			if (String.IsNullOrEmpty(customDeviceProperty.FullName)) throw new ArgumentException("customDeviceProperty.FullName cannot be null or empty.");

			lock (_Properties)
			{
				if (_Properties.ContainsKey(customDeviceProperty.FullName))
					return _Properties[customDeviceProperty.FullName] == customDeviceProperty;
			}

			return false;
		}

		/// <summary>
		/// Returns a boolean indicating whether or not a <see cref="SsdpDeviceProperty"/> instance with the specified full name value exists in the collection.
		/// </summary>
		/// <param name="customDevicePropertyFullName">A string containing the full name of the <see cref="SsdpDeviceProperty"/> instance to check for.</param>
		/// <returns>True if an item with the specified full name exists in the collection, otherwise false.</returns>
		public bool Contains(string customDevicePropertyFullName)
		{
			if (String.IsNullOrEmpty(customDevicePropertyFullName)) throw new ArgumentException("customDevicePropertyFullName cannot be null or empty.");

			lock (_Properties)
			{
				return _Properties.ContainsKey(customDevicePropertyFullName);
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Returns the number of items in the collection.
		/// </summary>
		public int Count
		{
			get { return _Properties.Count; }
		}

		/// <summary>
		/// Returns the <see cref="SsdpDeviceProperty"/> instance from the collection that has the specified <see cref="SsdpDeviceProperty.FullName"/> value.
		/// </summary>
		/// <param name="fullName">The full name of the property to return.</param>
		/// <returns>A <see cref="SsdpDeviceProperty"/> instance from the collection.</returns>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown if no item exists in the collection with the specified <paramref name="fullName"/> value.</exception>
		public SsdpDeviceProperty this[string fullName]
		{
			get 
			{
				return _Properties[fullName];
			}
		}

		#endregion

		#region IEnumerable<SsdpDeviceProperty> Members

		/// <summary>
		/// Returns an enumerator of <see cref="SsdpDeviceProperty"/> instances in this collection.
		/// </summary>
		/// <returns>An enumerator of <see cref="SsdpDeviceProperty"/> instances in this collection.</returns>
		public IEnumerator<SsdpDeviceProperty> GetEnumerator()
		{
			lock (_Properties)
			{
				return _Properties.Values.GetEnumerator();
			}
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator of <see cref="SsdpDeviceProperty"/> instances in this collection.
		/// </summary>
		/// <returns>An enumerator of <see cref="SsdpDeviceProperty"/> instances in this collection.</returns>
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			lock (_Properties)
			{
				return _Properties.Values.GetEnumerator();
			}
		}

		#endregion

	}
}