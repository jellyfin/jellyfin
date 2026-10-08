using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.HardwareAccelerationDevices
{
    /// <inheritdoc cref="IHardwareAccelerationDeviceManager"/>
    public sealed class HardwareAccelerationDeviceManager : IHardwareAccelerationDeviceManager, IDisposable
    {
        private readonly IDictionary<HardwareAccelerationType, IEnumerable<IHardwareAccelerationTypeDeviceDiscoverer>> _concreteHardwareAccelerationTypeDevicesDiscoverers;
        private readonly SemaphoreSlim _refreshSemaphor = new(1, 1);

        /// <summary>
        /// Initializes a new instance of the <see cref="HardwareAccelerationDeviceManager"/> class.
        /// </summary>
        /// <param name="concreteHardwareAccelerationTypeDevicesDiscoverers">Hardware acceleration type devices discoverers.</param>
        public HardwareAccelerationDeviceManager(
            IEnumerable<IHardwareAccelerationTypeDeviceDiscoverer> concreteHardwareAccelerationTypeDevicesDiscoverers)
        {
            _concreteHardwareAccelerationTypeDevicesDiscoverers = concreteHardwareAccelerationTypeDevicesDiscoverers
                .GroupBy(x => x.HardwareAccelerationType)
                .ToDictionary(x => x.Key, x => x.AsEnumerable());
        }

        /// <inheritdoc/>
        public IReadOnlyDictionary<HardwareAccelerationType, IEnumerable<HardwareAccelerationDevice>> Devices { get; private set; } = new Dictionary<HardwareAccelerationType, IEnumerable<HardwareAccelerationDevice>>();

        /// <inheritdoc/>
        public async Task RefreshAsync(CancellationToken cancellationToken)
        {
            await _refreshSemaphor.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                var devices = new Dictionary<HardwareAccelerationType, IEnumerable<HardwareAccelerationDevice>>();

                foreach (var concreteHardwareAccelerationTypeDevicesDiscoverer in _concreteHardwareAccelerationTypeDevicesDiscoverers)
                {
                    var foundDevices = (await Task.WhenAll(
                                concreteHardwareAccelerationTypeDevicesDiscoverer.Value
                                    .Select(x => x.DiscoverDevicesAsync(cancellationToken)))
                            .ConfigureAwait(false))
                        .SelectMany(x => x);

                    devices.Add(concreteHardwareAccelerationTypeDevicesDiscoverer.Key, foundDevices);
                }

                Devices = devices;
            }
            finally
            {
                _refreshSemaphor.Release();
            }
        }

        /// <inheritdoc/>
        public async Task RefreshConcreteTypeAsync(HardwareAccelerationType type, CancellationToken cancellationToken)
        {
            if (_concreteHardwareAccelerationTypeDevicesDiscoverers.TryGetValue(type, out var discoverers))
            {
                await _refreshSemaphor.WaitAsync(cancellationToken).ConfigureAwait(false);
                try
                {
                    var devices = Devices.ToDictionary();

                    IEnumerable<HardwareAccelerationDevice> foundDevices = Enumerable.Empty<HardwareAccelerationDevice>();

                    if (discoverers is not null)
                    {
                        foundDevices = (await Task.WhenAll(
                                    discoverers.Select(x => x.DiscoverDevicesAsync(cancellationToken)))
                                .ConfigureAwait(false))
                            .SelectMany(x => x);
                    }

                    devices[type] = foundDevices;

                    Devices = devices;
                }
                finally
                {
                    _refreshSemaphor.Release();
                }
            }
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            _refreshSemaphor.Dispose();
        }
    }
}
