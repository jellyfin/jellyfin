using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp
{
	/// <summary>
	/// Represents a custom property of an <see cref="SsdpDevice"/>.
	/// </summary>
	public sealed class SsdpDeviceProperty
	{

		/// <summary>
		/// Sets or returns the namespace this property exists in.
		/// </summary>
		public string Namespace { get; set; }

		/// <summary>
		/// Sets or returns the name of this property.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Returns the full name of this property (namespace and name).
		/// </summary>
		public string FullName { get { return String.IsNullOrEmpty(this.Namespace) ? this.Name : this.Namespace + ":" + this.Name; } }

		/// <summary>
		/// Sets or returns the value of this property.
		/// </summary>
		public string Value { get; set; }

	}
}
