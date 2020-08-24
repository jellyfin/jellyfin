#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using Emby.Dlna.PlayTo.EventArgs;

namespace Emby.Dlna.PlayTo.Devices
{
    /// <summary>
    /// Base class representing the common details of a (root or embedded) device, either to be published or that has been located.
    /// </summary>
    /// <remarks>
    /// <para>Do not derive new types directly from this class. New device classes should derive from either <see cref="SsdpRootDevice"/> or
    /// <see cref="SsdpEmbeddedDevice"/>.</para>
    /// </remarks>
    /// <seealso cref="SsdpRootDevice"/>
    /// <seealso cref="SsdpEmbeddedDevice"/>
    /// <remarks>
    /// Part of this code are taken from RSSDP.
    /// Copyright (c) 2015 Troy Willmot.
    /// Copyright (c) 2015-2018 Luke Pulverenti.
    /// </remarks>
    public abstract class SsdpDevice
    {
        private readonly IList<SsdpDevice> _devices;

        /// <summary>
        /// Initializes a new instance of the <see cref="SsdpDevice"/> class.
        /// Allows constructing a device with no parent. Should only be used from derived types that are or inherit
        /// from <see cref="SsdpRootDevice"/>.
        /// </summary>
        /// <param name="friendlyName">Friendly name.</param>
        /// <param name="manufacturer">Manufacturer.</param>
        /// <param name="modelName">Model name.</param>
        /// <param name="uuid">UDN.</param>
        protected SsdpDevice(string friendlyName, string manufacturer, string modelName, string uuid)
        {
            DeviceTypeNamespace = "schemas-upnp-org";
            DeviceType = "Basic";
            DeviceVersion = 1;

            _devices = new List<SsdpDevice>();
            this.Devices = new ReadOnlyCollection<SsdpDevice>(_devices);

            FriendlyName = friendlyName;
            ModelName = modelName;
            Manufacturer = manufacturer;
            Uuid = uuid;
        }

        /// <summary>
        /// Raised when a new child device is added.
        /// </summary>
        /// <seealso cref="AddDevice"/>
        /// <seealso cref="DeviceAdded"/>
        public event EventHandler<DeviceEventArgs>? DeviceAdded;

        /// <summary>
        /// Raised when a child device is removed.
        /// </summary>
        /// <seealso cref="RemoveDevice"/>
        /// <seealso cref="DeviceRemoved"/>
        public event EventHandler<DeviceEventArgs>? DeviceRemoved;

        /// <summary>
        /// Gets or sets the core device type (not including namespace, version etc.). Required.
        /// </summary>
        /// <remarks><para>Defaults to the UPnP basic device type.</para></remarks>
        /// <seealso cref="DeviceTypeNamespace"/>
        /// <seealso cref="DeviceVersion"/>
        /// <seealso cref="FullDeviceType"/>
        public string DeviceType { get; set; }

        /// <summary>
        /// Gets or sets the device class.
        /// </summary>
        public string? DeviceClass { get; set; }

        /// <summary>
        /// Gets or sets the namespace for the <see cref="DeviceType"/> of this device. Optional, but defaults to UPnP schema so should be
        /// changed if <see cref="DeviceType"/> is not a UPnP device type.
        /// </summary>
        /// <remarks><para>Defaults to the UPnP standard namespace.</para></remarks>
        /// <seealso cref="DeviceType"/>
        /// <seealso cref="DeviceVersion"/>
        /// <seealso cref="FullDeviceType"/>
        public string DeviceTypeNamespace { get; set; }

        /// <summary>
        /// Gets or sets the version of the device type. Optional, defaults to 1.
        /// </summary>
        /// <remarks><para>Defaults to a value of 1.</para></remarks>
        /// <seealso cref="DeviceType"/>
        /// <seealso cref="DeviceTypeNamespace"/>
        /// <seealso cref="FullDeviceType"/>
        public int DeviceVersion { get; set; }

        /// <summary>
        /// Gets the full device type string.
        /// </summary>
        /// <remarks>
        /// <para>The format used is urn:<see cref="DeviceTypeNamespace"/>:device:<see cref="DeviceType"/>:<see cref="DeviceVersion"/>.</para>
        /// </remarks>
        public string FullDeviceType
        {
            get
            {
                return string.Format(
                    CultureInfo.CurrentCulture,
                    "urn:{0}:{3}:{1}:{2}",
                    this.DeviceTypeNamespace ?? string.Empty,
                    this.DeviceType ?? string.Empty,
                    this.DeviceVersion,
                    this.DeviceClass ?? "device");
            }
        }

        /// <summary>
        /// Gets or sets the universally unique identifier for this device (without the uuid: prefix). Required.
        /// </summary>
        /// <remarks>
        /// <para>Must be the same over time for a specific device instance (i.e. must survive reboots).</para>
        /// <para>For UPnP 1.0 this can be any unique string. For UPnP 1.1 this should be a 128 bit number formatted in a specific way,
        /// preferably generated using the time and MAC based algorithm. See section 1.1.4 of http://upnp.org/specs/arch/UPnP-arch-DeviceArchitecture-v1.1.pdf
        /// for details.</para>
        /// <para>Technically this library implements UPnP 1.0, so any value is allowed, but we advise using UPnP 1.1 compatible values for good behaviour and
        /// forward compatibility with future versions.</para>
        /// </remarks>
        public string Uuid { get; set; }

        /// <summary>
        /// Gets a unique device name for this device. Optional, not recommended to be explicitly set.
        /// </summary>
        public string Udn
        {
            get
            {
                return "uuid:" + this.Uuid;
            }
        }

        /// <summary>
        /// Gets or sets a friendly/display name for this device on the network. Something the user can identify the device/instance by,
        /// i.e Lounge Main Light. Required.
        /// </summary>
        /// <remarks><para>A short description for the end user. </para></remarks>
        public string FriendlyName { get; set; }

        /// <summary>
        /// Gets or sets the name of the manufacturer of this device. Required.
        /// </summary>
        public string Manufacturer { get; set; }

        /// <summary>
        /// Gets or sets a URL to the manufacturers web site. Optional.
        /// </summary>
        public Uri? ManufacturerUrl { get; set; }

        /// <summary>
        /// Gets or sets a description of this device model. Recommended.
        /// </summary>
        /// <remarks><para>A long description for the end user.</para></remarks>
        public string? ModelDescription { get; set; }

        /// <summary>
        /// Gets or sets the name of this model. Required.
        /// </summary>
        public string ModelName { get; set; }

        /// <summary>
        /// Gets or sets the number of this model. Recommended.
        /// </summary>
        public string? ModelNumber { get; set; }

        /// <summary>
        /// Gets or sets a URL to a web page with details of this device model. Optional.
        /// </summary>
        /// <remarks>
        /// <para>Optional. May be relative to base URL.</para>
        /// </remarks>
        public Uri? ModelUrl { get; set; }

        /// <summary>
        /// Gets or sets the serial number for this device. Recommended.
        /// </summary>
        public string? SerialNumber { get; set; }

        /// <summary>
        /// Gets or sets the universal product code of the device, if any. Optional.
        /// </summary>
        /// <remarks>
        /// <para>If not blank, must be exactly 12 numeric digits.</para>
        /// </remarks>
        public string? Upc { get; set; }

        /// <summary>
        /// Gets or sets the URL to a web page that can be used to configure/manager/use the device. Recommended.
        /// </summary>
        /// <remarks>
        /// <para>May be relative to base URL. </para>
        /// </remarks>
        public Uri? PresentationUrl { get; set; }

        /// <summary>
        /// Gets a read-only enumerable set of <see cref="SsdpDevice"/> objects representing children of this device. Child devices are optional.
        /// </summary>
        /// <seealso cref="AddDevice"/>
        /// <seealso cref="RemoveDevice"/>
        public IList<SsdpDevice> Devices { get; }

        /// <summary>
        /// Returns the root device of this object.
        /// </summary>
        /// <returns>Root device in the structure.</returns>
        public SsdpRootDevice ToRootDevice()
        {
            SsdpDevice? rootDevice = null;
            if (this is SsdpRootDevice root)
            {
                rootDevice = root;
            }
            else if (this is SsdpEmbeddedDevice embedded)
            {
                rootDevice = embedded?.RootDevice;
            }

            if (rootDevice == null)
            {
                throw new NullReferenceException("RootDevice cannot be null.");
            }

            return (SsdpRootDevice)rootDevice;
        }

        /// <summary>
        /// Adds a child device to the <see cref="Devices"/> collection.
        /// </summary>
        /// <param name="device">The <see cref="SsdpEmbeddedDevice"/> instance to add.</param>
        /// <remarks>
        /// <para>If the device is already a member of the <see cref="Devices"/> collection, this method does nothing.</para>
        /// <para>Also sets the <see cref="SsdpEmbeddedDevice.RootDevice"/> property of the added device and all descendant devices to the
        /// relevant <see cref="SsdpRootDevice"/> instance.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown if the <paramref name="device"/> is already associated with a different
        /// <see cref="SsdpRootDevice"/> instance than used in this tree. Can occur if you try to add the same device instance to more than
        /// one tree. Also thrown if you try to add a device to itself.</exception>
        /// <seealso cref="DeviceAdded"/>
        public void AddDevice(SsdpEmbeddedDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            SsdpDevice thisRootDevice = ToRootDevice();
            if (device.RootDevice != null && device.RootDevice != thisRootDevice)
            {
                throw new InvalidOperationException("This device is already associated with a different root device (has been added as a child in another branch).");
            }

            if (device == this)
            {
                throw new InvalidOperationException("Can't add device to itself.");
            }

            bool wasAdded = false;
            lock (_devices)
            {
                device.RootDevice = thisRootDevice as SsdpRootDevice;
                if (!_devices.Contains(device))
                {
                    _devices.Add(device);
                    wasAdded = true;
                }
            }

            if (wasAdded)
            {
                OnDeviceAdded(device);
            }
        }

        /// <summary>
        /// Removes a child device from the <see cref="Devices"/> collection.
        /// </summary>
        /// <param name="device">The <see cref="SsdpEmbeddedDevice"/> instance to remove.</param>
        /// <remarks>
        /// <para>If the device is not a member of the <see cref="Devices"/> collection, this method does nothing.</para>
        /// <para>Also sets the <see cref="SsdpEmbeddedDevice.RootDevice"/> property to null for the removed device and all descendant devices.</para>
        /// </remarks>
        /// <exception cref="ArgumentNullException">Thrown if the <paramref name="device"/> argument is null.</exception>
        /// <seealso cref="DeviceRemoved"/>
        public void RemoveDevice(SsdpEmbeddedDevice device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            bool wasRemoved = false;
            lock (_devices)
            {
                wasRemoved = _devices.Remove(device);
                if (wasRemoved)
                {
                    device.RootDevice = null;
                }
            }

            if (wasRemoved)
            {
                OnDeviceRemoved(device);
            }
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"{DeviceType} - {Uuid}";
        }

        /// <summary>
        /// Changes the root of a device.
        /// </summary>
        /// <param name="value">New root device.</param>
        protected void ChangeRoot(SsdpRootDevice? value)
        {
            lock (_devices)
            {
                foreach (var embeddedDevice in this.Devices)
                {
                    ((SsdpEmbeddedDevice)embeddedDevice).RootDevice = value;
                }
            }
        }

        /// <summary>
        /// Raises the <see cref="DeviceAdded"/> event.
        /// </summary>
        /// <param name="device">The <see cref="SsdpEmbeddedDevice"/> instance added to the <see cref="Devices"/> collection.</param>
        /// <seealso cref="AddDevice"/>
        /// <seealso cref="DeviceAdded"/>
        protected virtual void OnDeviceAdded(SsdpEmbeddedDevice device)
        {
            this.DeviceAdded?.Invoke(this, new DeviceEventArgs(device));
        }

        /// <summary>
        /// Raises the <see cref="DeviceRemoved"/> event.
        /// </summary>
        /// <param name="device">The <see cref="SsdpEmbeddedDevice"/> instance removed from the <see cref="Devices"/> collection.</param>
        /// <seealso cref="RemoveDevice"/>
        /// <see cref="DeviceRemoved"/>
        protected virtual void OnDeviceRemoved(SsdpEmbeddedDevice device)
        {
            this.DeviceRemoved?.Invoke(this, new DeviceEventArgs(device));
        }
    }
}
