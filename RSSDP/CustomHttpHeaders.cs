using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rssdp
{
	/// <summary>
	/// Represents a custom HTTP header sent on device search response or notification messages.
	/// </summary>
	public sealed class CustomHttpHeader
	{

		#region Fields

		private string _Name;
		private string _Value;

		#endregion

		#region Constructors
		
		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="name">The field name of the header.</param>
		/// <param name="value">The value of the header</param>
		/// <remarks>
		/// <para>As per RFC 822 and 2616, the name must contain only printable ASCII characters (33-126) excluding colon (:). The value may contain any ASCII characters except carriage return or line feed.</para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">Thrown if the name is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the name is an empty value, or contains an invalid character. Also thrown if the value contains a \r or \n character.</exception>
		public CustomHttpHeader(string name, string value)
		{
			Name = name;
			Value = value;
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Return the name of this header.
		/// </summary>
		public string Name
		{
			get { return _Name; }
			private set
			{
				EnsureValidName(value);
				_Name = value;
			}
		}

		/// <summary>
		/// Returns the value of this header.
		/// </summary>
		public string Value
		{
			get { return _Value; }
			private set
			{
				EnsureValidValue(value);
				_Value = value;
			}
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Returns the header formatted for use in an HTTP message.
		/// </summary>
		/// <returns>A string representing this header in the format of  'name: value'.</returns>
		public override string ToString()
		{
			return this.Name + ": " + this.Value;
		}

		#endregion

		#region Private Methods

		private static void EnsureValidName(string name)
		{
			if (name == null) throw new ArgumentNullException(nameof(name), "Name cannot be null.");
			if (name.Length == 0) throw new ArgumentException("Name cannot be blank.", nameof(name));

			foreach (var c in name)
			{
				var b = (byte)c;
				if (c == ':' || b < 33 || b > 126) throw new ArgumentException("Name contains illegal characters.", nameof(name));
			}
		}

		private static void EnsureValidValue(string value)
		{
			if (String.IsNullOrEmpty(value)) return;

			if (value.Contains("\r") || value.Contains("\n")) throw new ArgumentException("Invalid value.", nameof(value));
		}

		#endregion

	}

	/// <summary>
	/// Represents a collection of custom HTTP headers, keyed by name.
	/// </summary>
	public class CustomHttpHeadersCollection : IEnumerable<CustomHttpHeader>
	{
		#region Fields

		private IDictionary<string, CustomHttpHeader> _Headers;

		#endregion
		
		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public CustomHttpHeadersCollection()
		{
			_Headers = new Dictionary<string, CustomHttpHeader>();
		}

		/// <summary>
		/// Full constructor.
		/// </summary>
		/// <param name="capacity">Specifies the initial capacity of the collection.</param>
		public CustomHttpHeadersCollection(int capacity)
		{
			_Headers = new Dictionary<string, CustomHttpHeader>(capacity);
		}

		#endregion

		#region Public Methpds

		/// <summary>
		/// Adds a <see cref="CustomHttpHeader"/> instance to the collection.
		/// </summary>
		/// <param name="header">The <see cref="CustomHttpHeader"/> instance to add to the collection.</param>
		/// <remarks>
		/// <para></para>
		/// </remarks>
		/// <exception cref="System.ArgumentNullException">Thrown if <paramref name="header"/> is null.</exception>
		public void Add(CustomHttpHeader header)
		{
			if (header == null) throw new ArgumentNullException(nameof(header));

			lock (_Headers)
			{
				_Headers.Add(header.Name, header);
			}
		}

		#region Remove Overloads

		/// <summary>
		/// Removes the specified header instance from the collection.
		/// </summary>
		/// <param name="header">The <see cref="CustomHttpHeader"/> instance to remove from the collection.</param>
		/// <remarks>
		/// <para>Only removes the specified header if that instance was in the collection, if another header with the same name exists in the collection it is not removed.</para>
		/// </remarks>
		/// <returns>True if an item was removed from the collection, otherwise false (because it did not exist or was not the same instance).</returns>
		/// <seealso cref="Remove(string)"/>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="header"/> is null.</exception>
		public bool Remove(CustomHttpHeader header)
		{
			if (header == null) throw new ArgumentNullException(nameof(header));

			lock (_Headers)
			{
				if (_Headers.ContainsKey(header.Name) && _Headers[header.Name] == header)
					return _Headers.Remove(header.Name);
			}

			return false;
		}

		/// <summary>
		/// Removes the property with the specified key (<see cref="CustomHttpHeader.Name"/> from the collection.
		/// </summary>
		/// <param name="headerName">The name of the <see cref="CustomHttpHeader"/> instance to remove from the collection.</param>
		/// <returns>True if an item was removed from the collection, otherwise false (because no item exists in the collection with that key).</returns> 
		/// <exception cref="System.ArgumentException">Thrown if the <paramref name="headerName"/> argument is null or empty string.</exception>
		public bool Remove(string headerName)
		{
			if (String.IsNullOrEmpty(headerName)) throw new ArgumentException("headerName cannot be null or empty.", nameof(headerName));

			lock (_Headers)
			{
				return _Headers.Remove(headerName);
			}
		}

		#endregion

		/// <summary>
		/// Returns a boolean indicating whether or not the specified <see cref="CustomHttpHeader"/> instance is in the collection.
		/// </summary>
		/// <param name="header">An <see cref="CustomHttpHeader"/> instance to check the collection for.</param>
		/// <returns>True if the specified instance exists in the collection, otherwise false.</returns>
		public bool Contains(CustomHttpHeader header)
		{
			if (header == null) throw new ArgumentNullException(nameof(header));

			lock (_Headers)
			{
				if (_Headers.ContainsKey(header.Name))
					return _Headers[header.Name] == header;
			}

			return false;
		}

		/// <summary>
		/// Returns a boolean indicating whether or not a <see cref="CustomHttpHeader"/> instance with the specified full name value exists in the collection.
		/// </summary>
		/// <param name="headerName">A string containing the full name of the <see cref="CustomHttpHeader"/> instance to check for.</param>
		/// <returns>True if an item with the specified full name exists in the collection, otherwise false.</returns>
		public bool Contains(string headerName)
		{
			if (String.IsNullOrEmpty(headerName)) throw new ArgumentException("headerName cannot be null or empty.", nameof(headerName));

			lock (_Headers)
			{
				return _Headers.ContainsKey(headerName);
			}
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Returns the number of items in the collection.
		/// </summary>
		public int Count
		{
			get { return _Headers.Count; }
		}

		/// <summary>
		/// Returns the <see cref="CustomHttpHeader"/> instance from the collection that has the specified <see cref="CustomHttpHeader.Name"/> value.
		/// </summary>
		/// <param name="name">The full name of the property to return.</param>
		/// <returns>A <see cref="CustomHttpHeader"/> instance from the collection.</returns>
		/// <exception cref="System.Collections.Generic.KeyNotFoundException">Thrown if no item exists in the collection with the specified <paramref name="name"/> value.</exception>
		public CustomHttpHeader this[string name]
		{
			get
			{
				return _Headers[name];
			}
		}

		#endregion

		#region IEnumerable Members

		/// <summary>
		/// Returns an enumerator of <see cref="CustomHttpHeader"/> instances in this collection.
		/// </summary>
		/// <returns>An enumerator of <see cref="CustomHttpHeader"/> instances in this collection.</returns>
		public IEnumerator<CustomHttpHeader> GetEnumerator()
		{
			lock (_Headers)
			{
				return _Headers.Values.GetEnumerator();
			}
		}

		/// <summary>
		/// Returns an enumerator of <see cref="CustomHttpHeader"/> instances in this collection.
		/// </summary>
		/// <returns>An enumerator of <see cref="CustomHttpHeader"/> instances in this collection.</returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			lock (_Headers)
			{
				return _Headers.Values.GetEnumerator();
			}
		}

		#endregion

	}
}