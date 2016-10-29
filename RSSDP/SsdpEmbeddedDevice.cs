using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rssdp
{
	/// <summary>
	/// Represents a device that is a descendant of a <see cref="SsdpRootDevice"/> instance.
	/// </summary>
	public class SsdpEmbeddedDevice : SsdpDevice
	{

		#region Fields
		
		private SsdpRootDevice _RootDevice;

		#endregion

		#region Constructors

		/// <summary>
		/// Default constructor.
		/// </summary>
		public SsdpEmbeddedDevice()
		{
		}

		/// <summary>
		/// Deserialisation constructor.
		/// </summary>
		/// <param name="deviceDescriptionXml">A UPnP device description XML document.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="deviceDescriptionXml"/> argument is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if the <paramref name="deviceDescriptionXml"/> argument is empty.</exception>
		public SsdpEmbeddedDevice(string deviceDescriptionXml)
			: base(deviceDescriptionXml)
		{
		}

		#endregion

		#region Public Properties

		/// <summary>
		/// Returns the <see cref="SsdpRootDevice"/> that is this device's first ancestor. If this device is itself an <see cref="SsdpRootDevice"/>, then returns a reference to itself.
		/// </summary>
		public SsdpRootDevice RootDevice
		{
			get
			{
				return _RootDevice;
			}
			internal set
			{
				_RootDevice = value;
				lock (this.Devices)
				{
					foreach (var embeddedDevice in this.Devices)
					{
						((SsdpEmbeddedDevice)embeddedDevice).RootDevice = _RootDevice;
					}
				}
			}
		}

		#endregion

	}
}