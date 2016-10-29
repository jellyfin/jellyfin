using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rssdp.Infrastructure
{
	/// <summary>
	/// Validates a <see cref="SsdpDevice"/> object's properties meet the UPnP 1.0 specification.
	/// </summary>
	/// <remarks>
	/// <para>This is a best effort validation for known rules, it doesn't guarantee 100% compatibility with the specification. Reading the specification yourself is the best way to ensure compatibility.</para>
	/// </remarks>
	public class Upnp10DeviceValidator : IUpnpDeviceValidator
	{

		#region Public Methods

		/// <summary>
		/// Returns an enumerable set of strings, each one being a description of an invalid property on the specified root device.
		/// </summary>
		/// <remarks>
		/// <para>If no errors are found, an empty (but non-null) enumerable is returned.</para>
		/// </remarks>
		/// <param name="device">The <see cref="SsdpRootDevice"/> to validate.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
		/// <returns>A non-null enumerable set of strings, empty if there are no validation errors, otherwise each string represents a discrete problem.</returns>
		public IEnumerable<string> GetValidationErrors(SsdpRootDevice device)
		{
			if (device == null) throw new ArgumentNullException("device");

			var retVal = GetValidationErrors((SsdpDevice)device) as IList<string>;

			if (device.Location == null)
				retVal.Add("Location cannot be null.");
			else if (!device.Location.IsAbsoluteUri)
				retVal.Add("Location must be an absolute URL.");

			return retVal;
		}

		/// <summary>
		/// Returns an enumerable set of strings, each one being a description of an invalid property on the specified device.
		/// </summary>
		/// <remarks>
		/// <para>If no errors are found, an empty (but non-null) enumerable is returned.</para>
		/// </remarks>
		/// <param name="device">The <see cref="SsdpDevice"/> to validate.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
		/// <returns>A non-null enumerable set of strings, empty if there are no validation errors, otherwise each string represents a discrete problem.</returns>
		public IEnumerable<string> GetValidationErrors(SsdpDevice device)
		{
			if (device == null) throw new ArgumentNullException("device");

			var retVal = new List<string>();

			if (String.IsNullOrEmpty(device.Uuid))
				retVal.Add("Uuid is not set.");

			if (!String.IsNullOrEmpty(device.Upc))
				ValidateUpc(device, retVal);

			if (String.IsNullOrEmpty(device.Udn))
				retVal.Add("UDN is not set.");
			else
				ValidateUdn(device, retVal);

			if (String.IsNullOrEmpty(device.DeviceType))
				retVal.Add("DeviceType is not set.");

			if (String.IsNullOrEmpty(device.DeviceTypeNamespace))
				retVal.Add("DeviceTypeNamespace is not set.");
			else
			{
				if (IsOverLength(device.DeviceTypeNamespace, 64))
					retVal.Add("DeviceTypeNamespace cannot be longer than 64 characters.");

				//if (device.DeviceTypeNamespace.Contains("."))
				//	retVal.Add("Period (.) characters in the DeviceTypeNamespace property must be replaced with hyphens (-).");
			}

			if (device.DeviceVersion <= 0)
				retVal.Add("DeviceVersion must be 1 or greater.");

			if (IsOverLength(device.ModelName, 32))
				retVal.Add("ModelName cannot be longer than 32 characters.");

			if (IsOverLength(device.ModelNumber, 32))
				retVal.Add("ModelNumber cannot be longer than 32 characters.");

			if (IsOverLength(device.FriendlyName, 64))
				retVal.Add("FriendlyName cannot be longer than 64 characters.");

			if (IsOverLength(device.Manufacturer, 64))
				retVal.Add("Manufacturer cannot be longer than 64 characters.");

			if (IsOverLength(device.SerialNumber, 64))
				retVal.Add("SerialNumber cannot be longer than 64 characters.");

			if (IsOverLength(device.ModelDescription, 128))
				retVal.Add("ModelDescription cannot be longer than 128 characters.");

			if (String.IsNullOrEmpty(device.FriendlyName))
				retVal.Add("FriendlyName is required.");

			if (String.IsNullOrEmpty(device.Manufacturer))
				retVal.Add("Manufacturer is required.");

			if (String.IsNullOrEmpty(device.ModelName))
				retVal.Add("ModelName is required.");

			if (device.Icons.Any())
				ValidateIcons(device, retVal);

			ValidateChildDevices(device, retVal);

			return retVal;
		}

		/// <summary>
		/// Validates the specified device and throws an <see cref="System.InvalidOperationException"/> if there are any validation errors.
		/// </summary>
		/// <param name="device">The <see cref="SsdpDevice"/> to validate.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
		/// <exception cref="System.InvalidOperationException">Thrown if the device object does not pass validation.</exception>
		public void ThrowIfDeviceInvalid(SsdpDevice device)
		{
			var errors = this.GetValidationErrors(device);
			if (errors != null && errors.Any()) throw new InvalidOperationException("Invalid device settings : " + String.Join(Environment.NewLine, errors));
		}

		#endregion

		#region Private Methods

		private static void ValidateUpc(SsdpDevice device, List<string> retVal)
		{
			if (device.Upc.Length != 12)
				retVal.Add("Upc, if provided, should be 12 digits.");

			foreach (char c in device.Upc)
			{
				if (!Char.IsDigit(c))
				{
					retVal.Add("Upc, if provided, should contain only digits (numeric characters).");
					break;
				}
			}
		}

		private static void ValidateUdn(SsdpDevice device, List<string> retVal)
		{
			if (!device.Udn.StartsWith("uuid:", StringComparison.OrdinalIgnoreCase))
				retVal.Add("UDN must begin with uuid:. Correct format is uuid:<uuid>");
			else if (device.Udn.Substring(5).Trim() != device.Uuid)
				retVal.Add("UDN incorrect. Correct format is uuid:<uuid>");
		}

		private static void ValidateIcons(SsdpDevice device, List<string> retVal)
		{
			if (device.Icons.Any((di) => di.Url == null))
				retVal.Add("Device icon is missing URL.");

			if (device.Icons.Any((di) => String.IsNullOrEmpty(di.MimeType)))
				retVal.Add("Device icon is missing mime type.");

			if (device.Icons.Any((di) => di.Width <= 0 || di.Height <= 0))
				retVal.Add("Device icon has zero (or negative) height, width or both.");

			if (device.Icons.Any((di) => di.ColorDepth <= 0))
				retVal.Add("Device icon has zero (or negative) colordepth.");
		}

		private void ValidateChildDevices(SsdpDevice device, List<string> retVal)
		{
			foreach (var childDevice in device.Devices)
			{
				foreach (var validationError in this.GetValidationErrors(childDevice))
				{
					retVal.Add("Embedded Device : " + childDevice.Uuid + ": " + validationError);
				}
			}
		}
		
		private static bool IsOverLength(string value, int maxLength)
		{
			return !String.IsNullOrEmpty(value) && value.Length > maxLength;
		}

		#endregion

	}
}
