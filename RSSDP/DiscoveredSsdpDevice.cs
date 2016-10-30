using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http.Headers;

namespace Rssdp
{
	/// <summary>
	/// Represents a discovered device, containing basic information about the device and the location of it's full device description document. Also provides convenience methods for retrieving the device description document.
	/// </summary>
	/// <seealso cref="SsdpDevice"/>
	/// <seealso cref="Rssdp.Infrastructure.ISsdpDeviceLocator"/>
	public sealed class DiscoveredSsdpDevice
	{

		#region Fields

		private SsdpRootDevice _Device;
		private DateTimeOffset _AsAt;

		private static HttpClient s_DefaultHttpClient;

		#endregion

		#region Public Properties

		/// <summary>
		/// Sets or returns the type of notification, being either a uuid, device type, service type or upnp:rootdevice.
		/// </summary>
		public string NotificationType { get; set; }

		/// <summary>
		/// Sets or returns the universal service name (USN) of the device.
		/// </summary>
		public string Usn { get; set; }

		/// <summary>
		/// Sets or returns a URL pointing to the device description document for this device.
		/// </summary>
		public Uri DescriptionLocation { get; set; }

		/// <summary>
		/// Sets or returns the length of time this information is valid for (from the <see cref="AsAt"/> time).
		/// </summary>
		public TimeSpan CacheLifetime { get; set; }

		/// <summary>
		/// Sets or returns the date and time this information was received.
		/// </summary>
		public DateTimeOffset AsAt
		{
			get { return _AsAt; }
			set
			{
				if (_AsAt != value)
				{
					_AsAt = value;
					_Device = null;
				}
			}
		}

		/// <summary>
		/// Returns the headers from the SSDP device response message
		/// </summary>
		public HttpHeaders ResponseHeaders { get; set; }

		#endregion

		#region Public Methods

		/// <summary>
		/// Returns true if this device information has expired, based on the current date/time, and the <see cref="CacheLifetime"/> &amp; <see cref="AsAt"/> properties.
		/// </summary>
		/// <returns></returns>
		public bool IsExpired()
		{
			return this.CacheLifetime == TimeSpan.Zero || this.AsAt.Add(this.CacheLifetime) <= DateTimeOffset.Now;
		}

		/// <summary>
		/// Retrieves the device description document specified by the <see cref="DescriptionLocation"/> property.
		/// </summary>
		/// <remarks>
		/// <para>This method may choose to cache (or return cached) information if called multiple times within the <see cref="CacheLifetime"/> period.</para>
		/// </remarks>
		/// <returns>An <see cref="SsdpDevice"/> instance describing the full device details.</returns>
		public async Task<SsdpDevice> GetDeviceInfo()
		{
			var device = _Device;
			if (device == null || this.IsExpired())
				return await GetDeviceInfo(GetDefaultClient());
			else
				return device;
		}

		/// <summary>
		/// Retrieves the device description document specified by the <see cref="DescriptionLocation"/> property using the provided <see cref="System.Net.Http.HttpClient"/> instance.
		/// </summary>
		/// <remarks>
		/// <para>This method may choose to cache (or return cached) information if called multiple times within the <see cref="CacheLifetime"/> period.</para>
		/// <para>This method performs no error handling, if an exception occurs downloading or parsing the document it will be thrown to the calling code. Ensure you setup correct error handling for these scenarios.</para>
		/// </remarks>
		/// <param name="downloadHttpClient">A <see cref="System.Net.Http.HttpClient"/> to use when downloading the document data.</param>
		/// <returns>An <see cref="SsdpDevice"/> instance describing the full device details.</returns>
		public async Task<SsdpRootDevice> GetDeviceInfo(HttpClient downloadHttpClient)
		{
			if (_Device == null || this.IsExpired())
			{
				var rawDescriptionDocument = await downloadHttpClient.GetAsync(this.DescriptionLocation);
				rawDescriptionDocument.EnsureSuccessStatusCode();

				// Not using ReadAsStringAsync() here as some devices return the content type as utf-8 not UTF-8,
				// which causes an (unneccesary) exception.
				var data = await rawDescriptionDocument.Content.ReadAsByteArrayAsync();
				_Device = new SsdpRootDevice(this.DescriptionLocation, this.CacheLifetime, System.Text.UTF8Encoding.UTF8.GetString(data, 0, data.Length));
			}

			return _Device;
		}

		#endregion

		#region Overrides

		/// <summary>
		/// Returns the device's <see cref="Usn"/> value.
		/// </summary>
		/// <returns>A string containing the device's universal service name.</returns>
		public override string ToString()
		{
			return this.Usn;
		}

		#endregion

		#region Private Methods


		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope", Justification = "Can't call dispose on the handler since we pass it to the HttpClient, which outlives the scope of this method.")]
		private static HttpClient GetDefaultClient()
		{
			if (s_DefaultHttpClient == null)
			{
				var handler = new System.Net.Http.HttpClientHandler();
				try
				{
					if (handler.SupportsAutomaticDecompression)
						handler.AutomaticDecompression = System.Net.DecompressionMethods.Deflate | System.Net.DecompressionMethods.GZip;

					s_DefaultHttpClient = new HttpClient(handler);
				}
				catch
				{
					if (handler != null)
						handler.Dispose();

					throw;
				}
			}

			return s_DefaultHttpClient;
		}

		#endregion

	}
}