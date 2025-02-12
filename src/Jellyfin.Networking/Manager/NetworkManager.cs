using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Net;
using MediaBrowser.Model.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using static MediaBrowser.Controller.Extensions.ConfigurationExtensions;
using IConfigurationManager = MediaBrowser.Common.Configuration.IConfigurationManager;
using IPNetwork = Microsoft.AspNetCore.HttpOverrides.IPNetwork;

namespace Jellyfin.Networking.Manager;

/// <summary>
/// Class to take care of network interface management.
/// </summary>
public class NetworkManager : INetworkManager, IDisposable
{
    /// <summary>
    /// Threading lock for network properties.
    /// </summary>
    private readonly object _initLock;

    private readonly ILogger<NetworkManager> _logger;

    private readonly IConfigurationManager _configurationManager;

    private readonly IConfiguration _startupConfig;

    private readonly object _networkEventLock;

    /// <summary>
    /// Holds the published server URLs and the IPs to use them on.
    /// </summary>
    private IReadOnlyList<PublishedServerUriOverride> _publishedServerUrls;

    private IReadOnlyList<IPNetwork> _remoteAddressFilter;

    /// <summary>
    /// Used to stop "event-racing conditions".
    /// </summary>
    private bool _eventfire;

    /// <summary>
    /// List of all interface MAC addresses.
    /// </summary>
    private IReadOnlyList<PhysicalAddress> _macAddresses;

    /// <summary>
    /// Dictionary containing interface addresses and their subnets.
    /// </summary>
    private IReadOnlyList<IPData> _interfaces;

    /// <summary>
    /// Unfiltered user defined LAN subnets (<see cref="NetworkConfiguration.LocalNetworkSubnets"/>)
    /// or internal interface network subnets if undefined by user.
    /// </summary>
    private IReadOnlyList<IPNetwork> _lanSubnets;

    /// <summary>
    /// User defined list of subnets to excluded from the LAN.
    /// </summary>
    private IReadOnlyList<IPNetwork> _excludedSubnets;

    /// <summary>
    /// True if this object is disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="NetworkManager"/> class.
    /// </summary>
    /// <param name="configurationManager">The <see cref="IConfigurationManager"/> instance.</param>
    /// <param name="startupConfig">The <see cref="IConfiguration"/> instance holding startup parameters.</param>
    /// <param name="logger">Logger to use for messages.</param>
    public NetworkManager(IConfigurationManager configurationManager, IConfiguration startupConfig, ILogger<NetworkManager> logger)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(configurationManager);

        _logger = logger;
        _configurationManager = configurationManager;
        _startupConfig = startupConfig;
        _initLock = new();
        _interfaces = new List<IPData>();
        _macAddresses = new List<PhysicalAddress>();
        _publishedServerUrls = new List<PublishedServerUriOverride>();
        _networkEventLock = new object();
        _remoteAddressFilter = new List<IPNetwork>();

        _ = bool.TryParse(startupConfig[DetectNetworkChangeKey], out var detectNetworkChange);

        UpdateSettings(_configurationManager.GetNetworkConfiguration());

        if (detectNetworkChange)
        {
            NetworkChange.NetworkAddressChanged += OnNetworkAddressChanged;
            NetworkChange.NetworkAvailabilityChanged += OnNetworkAvailabilityChanged;
        }

        _configurationManager.NamedConfigurationUpdated += ConfigurationUpdated;
    }

    /// <summary>
    /// Event triggered on network changes.
    /// </summary>
    public event EventHandler? NetworkChanged;

    /// <summary>
    /// Gets or sets a value indicating whether testing is taking place.
    /// </summary>
    public static string MockNetworkSettings { get; set; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether IP4 is enabled.
    /// </summary>
    public bool IsIPv4Enabled => _configurationManager.GetNetworkConfiguration().EnableIPv4;

    /// <summary>
    /// Gets a value indicating whether IP6 is enabled.
    /// </summary>
    public bool IsIPv6Enabled => _configurationManager.GetNetworkConfiguration().EnableIPv6;

    /// <summary>
    /// Gets a value indicating whether is all IPv6 interfaces are trusted as internal.
    /// </summary>
    public bool TrustAllIPv6Interfaces { get; private set; }

    /// <summary>
    /// Gets the Published server override list.
    /// </summary>
    public IReadOnlyList<PublishedServerUriOverride> PublishedServerUrls => _publishedServerUrls;

    /// <inheritdoc/>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Handler for network change events.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">A <see cref="NetworkAvailabilityEventArgs"/> containing network availability information.</param>
    private void OnNetworkAvailabilityChanged(object? sender, NetworkAvailabilityEventArgs e)
    {
        _logger.LogDebug("Network availability changed.");
        HandleNetworkChange();
    }

    /// <summary>
    /// Handler for network change events.
    /// </summary>
    /// <param name="sender">Sender.</param>
    /// <param name="e">An <see cref="EventArgs"/>.</param>
    private void OnNetworkAddressChanged(object? sender, EventArgs e)
    {
        _logger.LogDebug("Network address change detected.");
        HandleNetworkChange();
    }

    /// <summary>
    /// Triggers our event, and re-loads interface information.
    /// </summary>
    private void HandleNetworkChange()
    {
        lock (_networkEventLock)
        {
            if (!_eventfire)
            {
                // As network events tend to fire one after the other only fire once every second.
                _eventfire = true;
                OnNetworkChange();
            }
        }
    }

    /// <summary>
    /// Waits for 2 seconds before re-initialising the settings, as typically these events fire multiple times in succession.
    /// </summary>
    private void OnNetworkChange()
    {
        try
        {
            Thread.Sleep(2000);
            var networkConfig = _configurationManager.GetNetworkConfiguration();
            if (IsIPv6Enabled && !Socket.OSSupportsIPv6)
            {
                UpdateSettings(networkConfig);
            }
            else
            {
                InitializeInterfaces();
                InitializeLan(networkConfig);
                EnforceBindSettings(networkConfig);
            }

            PrintNetworkInformation(networkConfig);
            NetworkChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _eventfire = false;
        }
    }

    /// <summary>
    /// Generate a list of all the interface ip addresses and submasks where that are in the active/unknown state.
    /// Generate a list of all active mac addresses that aren't loopback addresses.
    /// </summary>
    private void InitializeInterfaces()
    {
        lock (_initLock)
        {
            _logger.LogDebug("Refreshing interfaces.");

            var interfaces = new List<IPData>();
            var macAddresses = new List<PhysicalAddress>();

            try
            {
                var nics = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(i => i.OperationalStatus == OperationalStatus.Up);

                foreach (NetworkInterface adapter in nics)
                {
                    try
                    {
                        var ipProperties = adapter.GetIPProperties();
                        var mac = adapter.GetPhysicalAddress();

                        // Populate MAC list
                        if (adapter.NetworkInterfaceType != NetworkInterfaceType.Loopback && !PhysicalAddress.None.Equals(mac))
                        {
                            macAddresses.Add(mac);
                        }

                        // Populate interface list
                        foreach (var info in ipProperties.UnicastAddresses)
                        {
                            if (IsIPv4Enabled && info.Address.AddressFamily == AddressFamily.InterNetwork)
                            {
                                var interfaceObject = new IPData(info.Address, new IPNetwork(info.Address, info.PrefixLength), adapter.Name)
                                {
                                    Index = ipProperties.GetIPv4Properties().Index,
                                    Name = adapter.Name,
                                    SupportsMulticast = adapter.SupportsMulticast
                                };

                                interfaces.Add(interfaceObject);
                            }
                            else if (IsIPv6Enabled && info.Address.AddressFamily == AddressFamily.InterNetworkV6)
                            {
                                var interfaceObject = new IPData(info.Address, new IPNetwork(info.Address, info.PrefixLength), adapter.Name)
                                {
                                    Index = ipProperties.GetIPv6Properties().Index,
                                    Name = adapter.Name,
                                    SupportsMulticast = adapter.SupportsMulticast
                                };

                                interfaces.Add(interfaceObject);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        // Ignore error, and attempt to continue.
                        _logger.LogError(ex, "Error encountered parsing interfaces.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obtaining interfaces.");
            }

            // If no interfaces are found, fallback to loopback interfaces.
            if (interfaces.Count == 0)
            {
                _logger.LogWarning("No interface information available. Using loopback interface(s).");

                if (IsIPv4Enabled)
                {
                    interfaces.Add(new IPData(IPAddress.Loopback, NetworkConstants.IPv4RFC5735Loopback, "lo"));
                }

                if (IsIPv6Enabled)
                {
                    interfaces.Add(new IPData(IPAddress.IPv6Loopback, NetworkConstants.IPv6RFC4291Loopback, "lo"));
                }
            }

            _logger.LogDebug("Discovered {NumberOfInterfaces} interfaces.", interfaces.Count);
            _logger.LogDebug("Interfaces addresses: {Addresses}", interfaces.OrderByDescending(s => s.AddressFamily == AddressFamily.InterNetwork).Select(s => s.Address.ToString()));

            _macAddresses = macAddresses;
            _interfaces = interfaces;
        }
    }

    /// <summary>
    /// Initializes internal LAN cache.
    /// </summary>
    [MemberNotNull(nameof(_lanSubnets), nameof(_excludedSubnets))]
    private void InitializeLan(NetworkConfiguration config)
    {
        lock (_initLock)
        {
            _logger.LogDebug("Refreshing LAN information.");

            // Get configuration options
            var subnets = config.LocalNetworkSubnets;

            // If no LAN addresses are specified, all private subnets and Loopback are deemed to be the LAN
            if (!NetworkUtils.TryParseToSubnets(subnets, out var lanSubnets, false) || lanSubnets.Count == 0)
            {
                _logger.LogDebug("Using LAN interface addresses as user provided no LAN details.");

                var fallbackLanSubnets = new List<IPNetwork>();
                if (IsIPv6Enabled)
                {
                    fallbackLanSubnets.Add(NetworkConstants.IPv6RFC4291Loopback); // RFC 4291 (Loopback)
                    fallbackLanSubnets.Add(NetworkConstants.IPv6RFC4291SiteLocal); // RFC 4291 (Site local)
                    fallbackLanSubnets.Add(NetworkConstants.IPv6RFC4193UniqueLocal); // RFC 4193 (Unique local)
                }

                if (IsIPv4Enabled)
                {
                    fallbackLanSubnets.Add(NetworkConstants.IPv4RFC5735Loopback); // RFC 5735 (Loopback)
                    fallbackLanSubnets.Add(NetworkConstants.IPv4RFC1918PrivateClassA); // RFC 1918 (private Class A)
                    fallbackLanSubnets.Add(NetworkConstants.IPv4RFC1918PrivateClassB); // RFC 1918 (private Class B)
                    fallbackLanSubnets.Add(NetworkConstants.IPv4RFC1918PrivateClassC); // RFC 1918 (private Class C)
                }

                _lanSubnets = fallbackLanSubnets;
            }
            else
            {
                _lanSubnets = lanSubnets;
            }

            _excludedSubnets = NetworkUtils.TryParseToSubnets(subnets, out var excludedSubnets, true)
                ? excludedSubnets
                : new List<IPNetwork>();
        }
    }

    /// <summary>
    /// Enforce bind addresses and exclusions on available interfaces.
    /// </summary>
    private void EnforceBindSettings(NetworkConfiguration config)
    {
        lock (_initLock)
        {
            // Respect explicit bind addresses
            var interfaces = _interfaces.ToList();
            var localNetworkAddresses = config.LocalNetworkAddresses;
            if (localNetworkAddresses.Length > 0 && !string.IsNullOrWhiteSpace(localNetworkAddresses[0]))
            {
                var bindAddresses = localNetworkAddresses.Select(p => NetworkUtils.TryParseToSubnet(p, out var network)
                        ? network.Prefix
                        : (interfaces.Where(x => x.Name.Equals(p, StringComparison.OrdinalIgnoreCase))
                            .Select(x => x.Address)
                            .FirstOrDefault() ?? IPAddress.None))
                    .Where(x => x != IPAddress.None)
                    .ToHashSet();
                interfaces = interfaces.Where(x => bindAddresses.Contains(x.Address)).ToList();

                if (bindAddresses.Contains(IPAddress.Loopback) && !interfaces.Any(i => i.Address.Equals(IPAddress.Loopback)))
                {
                    interfaces.Add(new IPData(IPAddress.Loopback, NetworkConstants.IPv4RFC5735Loopback, "lo"));
                }

                if (bindAddresses.Contains(IPAddress.IPv6Loopback) && !interfaces.Any(i => i.Address.Equals(IPAddress.IPv6Loopback)))
                {
                    interfaces.Add(new IPData(IPAddress.IPv6Loopback, NetworkConstants.IPv6RFC4291Loopback, "lo"));
                }
            }

            // Remove all interfaces matching any virtual machine interface prefix
            if (config.IgnoreVirtualInterfaces)
            {
                // Remove potentially existing * and split config string into prefixes
                var virtualInterfacePrefixes = config.VirtualInterfaceNames
                    .Select(i => i.Replace("*", string.Empty, StringComparison.OrdinalIgnoreCase));

                // Check all interfaces for matches against the prefixes and remove them
                if (_interfaces.Count > 0)
                {
                    foreach (var virtualInterfacePrefix in virtualInterfacePrefixes)
                    {
                        interfaces.RemoveAll(x => x.Name.StartsWith(virtualInterfacePrefix, StringComparison.OrdinalIgnoreCase));
                    }
                }
            }

            // Remove all IPv4 interfaces if IPv4 is disabled
            if (!IsIPv4Enabled)
            {
                interfaces.RemoveAll(x => x.AddressFamily == AddressFamily.InterNetwork);
            }

            // Remove all IPv6 interfaces if IPv6 is disabled
            if (!IsIPv6Enabled)
            {
                interfaces.RemoveAll(x => x.AddressFamily == AddressFamily.InterNetworkV6);
            }

            // Users may have complex networking configuration that multiple interfaces sharing the same IP address
            // Only return one IP for binding, and let the OS handle the rest
            _interfaces = interfaces.DistinctBy(iface => iface.Address).ToList();
        }
    }

    /// <summary>
    /// Initializes the remote address values.
    /// </summary>
    private void InitializeRemote(NetworkConfiguration config)
    {
        lock (_initLock)
        {
            // Parse config values into filter collection
            var remoteIPFilter = config.RemoteIPFilter;
            if (remoteIPFilter.Length != 0 && !string.IsNullOrWhiteSpace(remoteIPFilter[0]))
            {
                // Parse all IPs with netmask to a subnet
                var remoteAddressFilter = new List<IPNetwork>();
                var remoteFilteredSubnets = remoteIPFilter.Where(x => x.Contains('/', StringComparison.OrdinalIgnoreCase)).ToArray();
                if (NetworkUtils.TryParseToSubnets(remoteFilteredSubnets, out var remoteAddressFilterResult, false))
                {
                    remoteAddressFilter = remoteAddressFilterResult.ToList();
                }

                // Parse everything else as an IP and construct subnet with a single IP
                var remoteFilteredIPs = remoteIPFilter.Where(x => !x.Contains('/', StringComparison.OrdinalIgnoreCase));
                foreach (var ip in remoteFilteredIPs)
                {
                    if (IPAddress.TryParse(ip, out var ipp))
                    {
                        remoteAddressFilter.Add(new IPNetwork(ipp, ipp.AddressFamily == AddressFamily.InterNetwork ? NetworkConstants.MinimumIPv4PrefixSize : NetworkConstants.MinimumIPv6PrefixSize));
                    }
                }

                _remoteAddressFilter = remoteAddressFilter;
            }
        }
    }

    /// <summary>
    /// Parses the user defined overrides into the dictionary object.
    /// Overrides are the equivalent of localised publishedServerUrl, enabling
    /// different addresses to be advertised over different subnets.
    /// format is subnet=ipaddress|host|uri
    /// when subnet = 0.0.0.0, any external address matches.
    /// </summary>
    private void InitializeOverrides(NetworkConfiguration config)
    {
        lock (_initLock)
        {
            var publishedServerUrls = new List<PublishedServerUriOverride>();

            // Prefer startup configuration.
            var startupOverrideKey = _startupConfig[AddressOverrideKey];
            if (!string.IsNullOrEmpty(startupOverrideKey))
            {
                publishedServerUrls.Add(
                    new PublishedServerUriOverride(
                        new IPData(IPAddress.Any, NetworkConstants.IPv4Any),
                        startupOverrideKey,
                        true,
                        true));
                publishedServerUrls.Add(
                    new PublishedServerUriOverride(
                        new IPData(IPAddress.IPv6Any, NetworkConstants.IPv6Any),
                        startupOverrideKey,
                        true,
                        true));
                _publishedServerUrls = publishedServerUrls;
                return;
            }

            var overrides = config.PublishedServerUriBySubnet;
            foreach (var entry in overrides)
            {
                var parts = entry.Split('=');
                if (parts.Length != 2)
                {
                    _logger.LogError("Unable to parse bind override: {Entry}", entry);
                    return;
                }

                var replacement = parts[1].Trim();
                var identifier = parts[0];
                if (string.Equals(identifier, "all", StringComparison.OrdinalIgnoreCase))
                {
                    // Drop any other overrides in case an "all" override exists
                    publishedServerUrls.Clear();
                    publishedServerUrls.Add(
                        new PublishedServerUriOverride(
                            new IPData(IPAddress.Any, NetworkConstants.IPv4Any),
                            replacement,
                            true,
                            true));
                    publishedServerUrls.Add(
                        new PublishedServerUriOverride(
                            new IPData(IPAddress.IPv6Any, NetworkConstants.IPv6Any),
                            replacement,
                            true,
                            true));
                    break;
                }
                else if (string.Equals(identifier, "external", StringComparison.OrdinalIgnoreCase))
                {
                    publishedServerUrls.Add(
                        new PublishedServerUriOverride(
                            new IPData(IPAddress.Any, NetworkConstants.IPv4Any),
                            replacement,
                            false,
                            true));
                    publishedServerUrls.Add(
                        new PublishedServerUriOverride(
                            new IPData(IPAddress.IPv6Any, NetworkConstants.IPv6Any),
                            replacement,
                            false,
                            true));
                }
                else if (string.Equals(identifier, "internal", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var lan in _lanSubnets)
                    {
                        var lanPrefix = lan.Prefix;
                        publishedServerUrls.Add(
                            new PublishedServerUriOverride(
                                new IPData(lanPrefix, new IPNetwork(lanPrefix, lan.PrefixLength)),
                                replacement,
                                true,
                                false));
                    }
                }
                else if (NetworkUtils.TryParseToSubnet(identifier, out var result) && result is not null)
                {
                    var data = new IPData(result.Prefix, result);
                    publishedServerUrls.Add(
                        new PublishedServerUriOverride(
                            data,
                            replacement,
                            true,
                            true));
                }
                else if (TryParseInterface(identifier, out var ifaces))
                {
                    foreach (var iface in ifaces)
                    {
                        publishedServerUrls.Add(
                            new PublishedServerUriOverride(
                                iface,
                                replacement,
                                true,
                                true));
                    }
                }
                else
                {
                    _logger.LogError("Unable to parse bind override: {Entry}", entry);
                }
            }

            _publishedServerUrls = publishedServerUrls;
        }
    }

    private void ConfigurationUpdated(object? sender, ConfigurationUpdateEventArgs evt)
    {
        if (evt.Key.Equals(NetworkConfigurationStore.StoreKey, StringComparison.Ordinal))
        {
            UpdateSettings((NetworkConfiguration)evt.NewConfiguration);
        }
    }

    /// <summary>
    /// Reloads all settings and re-Initializes the instance.
    /// </summary>
    /// <param name="configuration">The <see cref="NetworkConfiguration"/> to use.</param>
    [MemberNotNull(nameof(_lanSubnets), nameof(_excludedSubnets))]
    public void UpdateSettings(object configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var config = (NetworkConfiguration)configuration;
        HappyEyeballs.HttpClientExtension.UseIPv6 = config.EnableIPv6;

        InitializeLan(config);
        InitializeRemote(config);

        if (string.IsNullOrEmpty(MockNetworkSettings))
        {
            InitializeInterfaces();
        }
        else // Used in testing only.
        {
            // Format is <IPAddress>,<Index>,<Name>: <next interface>. Set index to -ve to simulate a gateway.
            var interfaceList = MockNetworkSettings.Split('|');
            var interfaces = new List<IPData>();
            foreach (var details in interfaceList)
            {
                var parts = details.Split(',');
                if (NetworkUtils.TryParseToSubnet(parts[0], out var subnet))
                {
                    var address = subnet.Prefix;
                    var index = int.Parse(parts[1], CultureInfo.InvariantCulture);
                    if (address.AddressFamily == AddressFamily.InterNetwork || address.AddressFamily == AddressFamily.InterNetworkV6)
                    {
                        var data = new IPData(address, subnet, parts[2])
                        {
                            Index = index
                        };
                        interfaces.Add(data);
                    }
                }
                else
                {
                    _logger.LogWarning("Could not parse mock interface settings: {Part}", details);
                }
            }

            _interfaces = interfaces;
        }

        EnforceBindSettings(config);
        InitializeOverrides(config);

        PrintNetworkInformation(config, false);
    }

    /// <summary>
    /// Protected implementation of Dispose pattern.
    /// </summary>
    /// <param name="disposing"><c>True</c> to dispose the managed state.</param>
    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _configurationManager.NamedConfigurationUpdated -= ConfigurationUpdated;
                NetworkChange.NetworkAddressChanged -= OnNetworkAddressChanged;
                NetworkChange.NetworkAvailabilityChanged -= OnNetworkAvailabilityChanged;
            }

            _disposed = true;
        }
    }

    /// <inheritdoc/>
    public bool TryParseInterface(string intf, [NotNullWhen(true)] out IReadOnlyList<IPData>? result)
    {
        if (string.IsNullOrEmpty(intf)
            || _interfaces is null
            || _interfaces.Count == 0)
        {
            result = null;
            return false;
        }

        // Match all interfaces starting with names starting with token
        result = _interfaces
            .Where(i => i.Name.Equals(intf, StringComparison.OrdinalIgnoreCase)
                        && ((IsIPv4Enabled && i.Address.AddressFamily == AddressFamily.InterNetwork)
                            || (IsIPv6Enabled && i.Address.AddressFamily == AddressFamily.InterNetworkV6)))
            .OrderBy(x => x.Index)
            .ToArray();
        return result.Count > 0;
    }

    /// <inheritdoc/>
    public bool HasRemoteAccess(IPAddress remoteIP)
    {
        var config = _configurationManager.GetNetworkConfiguration();
        if (config.EnableRemoteAccess)
        {
            // Comma separated list of IP addresses or IP/netmask entries for networks that will be allowed to connect remotely.
            // If left blank, all remote addresses will be allowed.
            if (_remoteAddressFilter.Any() && !IsInLocalNetwork(remoteIP))
            {
                // remoteAddressFilter is a whitelist or blacklist.
                var matches = _remoteAddressFilter.Count(remoteNetwork => SubnetContainsAddress(remoteNetwork, remoteIP));
                if ((!config.IsRemoteIPFilterBlacklist && matches > 0)
                    || (config.IsRemoteIPFilterBlacklist && matches == 0))
                {
                    return true;
                }

                return false;
            }
        }
        else if (!IsInLocalNetwork(remoteIP))
        {
            // Remote not enabled. So everyone should be LAN.
            return false;
        }

        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<PhysicalAddress> GetMacAddresses()
    {
        // Populated in construction - so always has values.
        return _macAddresses;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPData> GetLoopbacks()
    {
        if (!IsIPv4Enabled && !IsIPv6Enabled)
        {
            return Array.Empty<IPData>();
        }

        var loopbackNetworks = new List<IPData>();
        if (IsIPv4Enabled)
        {
            loopbackNetworks.Add(new IPData(IPAddress.Loopback, NetworkConstants.IPv4RFC5735Loopback, "lo"));
        }

        if (IsIPv6Enabled)
        {
            loopbackNetworks.Add(new IPData(IPAddress.IPv6Loopback, NetworkConstants.IPv6RFC4291Loopback, "lo"));
        }

        return loopbackNetworks;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPData> GetAllBindInterfaces(bool individualInterfaces = false)
    {
        var config = _configurationManager.GetNetworkConfiguration();
        var localNetworkAddresses = config.LocalNetworkAddresses;
        if ((localNetworkAddresses.Length > 0 && !string.IsNullOrWhiteSpace(localNetworkAddresses[0]) && _interfaces.Count > 0) || individualInterfaces)
        {
            return _interfaces;
        }

        // No bind address and no exclusions, so listen on all interfaces.
        var result = new List<IPData>();
        if (IsIPv4Enabled && IsIPv6Enabled)
        {
            // Kestrel source code shows it uses Sockets.DualMode - so this also covers IPAddress.Any by default
            result.Add(new IPData(IPAddress.IPv6Any, NetworkConstants.IPv6Any));
        }
        else if (IsIPv4Enabled)
        {
            result.Add(new IPData(IPAddress.Any, NetworkConstants.IPv4Any));
        }
        else if (IsIPv6Enabled)
        {
            // Cannot use IPv6Any as Kestrel will bind to IPv4 addresses too.
            foreach (var iface in _interfaces)
            {
                if (iface.AddressFamily == AddressFamily.InterNetworkV6)
                {
                    result.Add(iface);
                }
            }
        }

        return result;
    }

    /// <inheritdoc/>
    public string GetBindAddress(string source, out int? port)
    {
        if (!NetworkUtils.TryParseHost(source, out var addresses, IsIPv4Enabled, IsIPv6Enabled))
        {
            addresses = Array.Empty<IPAddress>();
        }

        var result = GetBindAddress(addresses.FirstOrDefault(), out port);
        return result;
    }

    /// <inheritdoc/>
    public string GetBindAddress(HttpRequest source, out int? port)
    {
        var result = GetBindAddress(source.Host.Host, out port);
        port ??= source.Host.Port;

        return result;
    }

    /// <inheritdoc/>
    public string GetBindAddress(IPAddress? source, out int? port, bool skipOverrides = false)
    {
        port = null;

        string result;

        if (source is not null)
        {
            if (IsIPv4Enabled && !IsIPv6Enabled && source.AddressFamily == AddressFamily.InterNetworkV6)
            {
                _logger.LogWarning("IPv6 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
            }

            if (!IsIPv4Enabled && IsIPv6Enabled && source.AddressFamily == AddressFamily.InterNetwork)
            {
                _logger.LogWarning("IPv4 is disabled in Jellyfin, but enabled in the OS. This may affect how the interface is selected.");
            }

            bool isExternal = !IsInLocalNetwork(source);
            _logger.LogDebug("Trying to get bind address for source {Source} - External: {IsExternal}", source, isExternal);

            if (!skipOverrides && MatchesPublishedServerUrl(source, isExternal, out result))
            {
                return result;
            }

            // No preference given, so move on to bind addresses.
            if (MatchesBindInterface(source, isExternal, out result))
            {
                return result;
            }

            if (isExternal && MatchesExternalInterface(source, out result))
            {
                return result;
            }
        }

        // Get the first LAN interface address that's not excluded and not a loopback address.
        // Get all available interfaces, prefer local interfaces
        var availableInterfaces = _interfaces.Where(x => !IPAddress.IsLoopback(x.Address))
            .OrderByDescending(x => IsInLocalNetwork(x.Address))
            .ThenBy(x => x.Index)
            .ToList();

        if (availableInterfaces.Count == 0)
        {
            // There isn't any others, so we'll use the loopback.
            result = IsIPv4Enabled && !IsIPv6Enabled ? "127.0.0.1" : "::1";
            _logger.LogWarning("{Source}: Only loopback {Result} returned, using that as bind address.", source, result);
            return result;
        }

        // If no source address is given, use the preferred (first) interface
        if (source is null)
        {
            result = NetworkUtils.FormatIPString(availableInterfaces.First().Address);
            _logger.LogDebug("{Source}: Using first internal interface as bind address: {Result}", source, result);
            return result;
        }

        // Does the request originate in one of the interface subnets?
        // (For systems with multiple internal network cards, and multiple subnets)
        foreach (var intf in availableInterfaces)
        {
            if (SubnetContainsAddress(intf.Subnet, source))
            {
                result = NetworkUtils.FormatIPString(intf.Address);
                _logger.LogDebug("{Source}: Found interface with matching subnet, using it as bind address: {Result}", source, result);
                return result;
            }
        }

        // Fallback to first available interface
        result = NetworkUtils.FormatIPString(availableInterfaces[0].Address);
        _logger.LogDebug("{Source}: No matching interfaces found, using preferred interface as bind address: {Result}", source, result);
        return result;
    }

    /// <inheritdoc/>
    public IReadOnlyList<IPData> GetInternalBindAddresses()
    {
        // Select all local bind addresses
        return _interfaces.Where(x => IsInLocalNetwork(x.Address))
            .OrderBy(x => x.Index)
            .ToList();
    }

    /// <inheritdoc/>
    public bool IsInLocalNetwork(string address)
    {
        if (NetworkUtils.TryParseToSubnet(address, out var subnet))
        {
            return IsInLocalNetwork(subnet.Prefix);
        }

        return NetworkUtils.TryParseHost(address, out var addresses, IsIPv4Enabled, IsIPv6Enabled)
               && addresses.Any(IsInLocalNetwork);
    }

    /// <summary>
    ///  Get if the IPAddress is Link-local.
    /// </summary>
    /// <param name="address">The IP Address.</param>
    /// <returns>Bool indicates if the address is link-local.</returns>
    public bool IsLinkLocalAddress(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        return NetworkConstants.IPv4RFC3927LinkLocal.Contains(address) || address.IsIPv6LinkLocal;
    }

    private static bool SubnetContainsAddress(IPNetwork network, IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);
        ArgumentNullException.ThrowIfNull(network);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        return network.Contains(address);
    }

    /// <inheritdoc/>
    public bool IsInLocalNetwork(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        // Map IPv6 mapped IPv4 back to IPv4 (happens if Kestrel runs in dual-socket mode)
        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if ((TrustAllIPv6Interfaces && address.AddressFamily == AddressFamily.InterNetworkV6)
            || IPAddress.IsLoopback(address))
        {
            return true;
        }

        // As private addresses can be redefined by Configuration.LocalNetworkAddresses
        return CheckIfLanAndNotExcluded(address);
    }

    /// <summary>
    /// Check if the address is in the LAN and not excluded.
    /// </summary>
    /// <param name="address">The IP address to check. The caller should make sure this is not an IPv4MappedToIPv6 address.</param>
    /// <returns>Boolean indicates whether the address is in LAN.</returns>
    private bool CheckIfLanAndNotExcluded(IPAddress address)
    {
        foreach (var lanSubnet in _lanSubnets)
        {
            if (lanSubnet.Contains(address))
            {
                foreach (var excludedSubnet in _excludedSubnets)
                {
                    if (excludedSubnet.Contains(address))
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to match the source against the published server URL overrides.
    /// </summary>
    /// <param name="source">IP source address to use.</param>
    /// <param name="isInExternalSubnet">True if the source is in an external subnet.</param>
    /// <param name="bindPreference">The published server URL that matches the source address.</param>
    /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
    private bool MatchesPublishedServerUrl(IPAddress source, bool isInExternalSubnet, out string bindPreference)
    {
        bindPreference = string.Empty;
        int? port = null;

        // Only consider subnets including the source IP, prefering specific overrides
        List<PublishedServerUriOverride> validPublishedServerUrls;
        if (!isInExternalSubnet)
        {
            // Only use matching internal subnets
            // Prefer more specific (bigger subnet prefix) overrides
            validPublishedServerUrls = _publishedServerUrls.Where(x => x.IsInternalOverride && SubnetContainsAddress(x.Data.Subnet, source))
                .OrderByDescending(x => x.Data.Subnet.PrefixLength)
                .ToList();
        }
        else
        {
            // Only use matching external subnets
            // Prefer more specific (bigger subnet prefix) overrides
            validPublishedServerUrls = _publishedServerUrls.Where(x => x.IsExternalOverride && SubnetContainsAddress(x.Data.Subnet, source))
                .OrderByDescending(x => x.Data.Subnet.PrefixLength)
                .ToList();
        }

        foreach (var data in validPublishedServerUrls)
        {
            // Get interface matching override subnet
            var intf = _interfaces.OrderBy(x => x.Index).FirstOrDefault(x => SubnetContainsAddress(data.Data.Subnet, x.Address));

            if (intf?.Address is not null
                || (data.Data.AddressFamily == AddressFamily.InterNetwork && data.Data.Address.Equals(IPAddress.Any))
                || (data.Data.AddressFamily == AddressFamily.InterNetworkV6 && data.Data.Address.Equals(IPAddress.IPv6Any)))
            {
                // If matching interface is found, use override
                bindPreference = data.OverrideUri;
                break;
            }
        }

        if (string.IsNullOrEmpty(bindPreference))
        {
            _logger.LogDebug("{Source}: No matching bind address override found", source);
            return false;
        }

        // Handle override specifying port
        var parts = bindPreference.Split(':');
        if (parts.Length > 1)
        {
            if (int.TryParse(parts[1], out int p))
            {
                bindPreference = parts[0];
                port = p;
                _logger.LogDebug("{Source}: Matching bind address override found: {Address}:{Port}", source, bindPreference, port);
                return true;
            }
        }

        _logger.LogDebug("{Source}: Matching bind address override found: {Address}", source, bindPreference);

        return true;
    }

    /// <summary>
    /// Attempts to match the source against the user defined bind interfaces.
    /// </summary>
    /// <param name="source">IP source address to use.</param>
    /// <param name="isInExternalSubnet">True if the source is in the external subnet.</param>
    /// <param name="result">The result, if a match is found.</param>
    /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
    private bool MatchesBindInterface(IPAddress source, bool isInExternalSubnet, out string result)
    {
        result = string.Empty;

        int count = _interfaces.Count;
        if (count == 1 && (_interfaces[0].Address.Equals(IPAddress.Any) || _interfaces[0].Address.Equals(IPAddress.IPv6Any)))
        {
            // Ignore IPAny addresses.
            count = 0;
        }

        if (count == 0)
        {
            return false;
        }

        IPAddress? bindAddress = null;
        if (isInExternalSubnet)
        {
            var externalInterfaces = _interfaces.Where(x => !IsInLocalNetwork(x.Address))
                .Where(x => !IsLinkLocalAddress(x.Address))
                .OrderBy(x => x.Index)
                .ToList();
            if (externalInterfaces.Count > 0)
            {
                // Check to see if any of the external bind interfaces are in the same subnet as the source.
                // If none exists, this will select the first external interface if there is one.
                bindAddress = externalInterfaces
                    .OrderByDescending(x => SubnetContainsAddress(x.Subnet, source))
                    .ThenByDescending(x => x.Subnet.PrefixLength)
                    .ThenBy(x => x.Index)
                    .Select(x => x.Address)
                    .First();

                result = NetworkUtils.FormatIPString(bindAddress);
                _logger.LogDebug("{Source}: External request received, matching external bind address found: {Result}", source, result);
                return true;
            }

            _logger.LogDebug("{Source}: External request received, no matching external bind address found, trying internal addresses", source);
        }
        else
        {
            // Check to see if any of the internal bind interfaces are in the same subnet as the source.
            // If none exists, this will select the first internal interface if there is one.
            bindAddress = _interfaces.Where(x => IsInLocalNetwork(x.Address))
                .OrderByDescending(x => SubnetContainsAddress(x.Subnet, source))
                .ThenByDescending(x => x.Subnet.PrefixLength)
                .ThenBy(x => x.Index)
                .Select(x => x.Address)
                .FirstOrDefault();

            if (bindAddress is not null)
            {
                result = NetworkUtils.FormatIPString(bindAddress);
                _logger.LogDebug("{Source}: Internal request received, matching internal bind address found: {Result}", source, result);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to match the source against external interfaces.
    /// </summary>
    /// <param name="source">IP source address to use.</param>
    /// <param name="result">The result, if a match is found.</param>
    /// <returns><c>true</c> if a match is found, <c>false</c> otherwise.</returns>
    private bool MatchesExternalInterface(IPAddress source, out string result)
    {
        // Get the first external interface address that isn't a loopback.
        var extResult = _interfaces
            .Where(p => !IsInLocalNetwork(p.Address))
            .Where(p => p.Address.AddressFamily.Equals(source.AddressFamily))
            .Where(p => !IsLinkLocalAddress(p.Address))
            .OrderBy(x => x.Index).ToArray();

        // No external interface found
        if (extResult.Length == 0)
        {
            result = string.Empty;
            _logger.LogDebug("{Source}: External request received, but no external interface found. Need to route through internal network", source);
            return false;
        }

        // Does the request originate in one of the interface subnets?
        // (For systems with multiple network cards and/or multiple subnets)
        foreach (var intf in extResult)
        {
            if (SubnetContainsAddress(intf.Subnet, source))
            {
                result = NetworkUtils.FormatIPString(intf.Address);
                _logger.LogDebug("{Source}: Found external interface with matching subnet, using it as bind address: {Result}", source, result);
                return true;
            }
        }

        // Fallback to first external interface.
        result = NetworkUtils.FormatIPString(extResult[0].Address);
        _logger.LogDebug("{Source}: Using first external interface as bind address: {Result}", source, result);
        return true;
    }

    private void PrintNetworkInformation(NetworkConfiguration config, bool debug = true)
    {
        var logLevel = debug ? LogLevel.Debug : LogLevel.Information;
        if (_logger.IsEnabled(logLevel))
        {
            _logger.Log(logLevel, "Defined LAN subnets: {Subnets}", _lanSubnets.Select(s => s.Prefix + "/" + s.PrefixLength));
            _logger.Log(logLevel, "Defined LAN exclusions: {Subnets}", _excludedSubnets.Select(s => s.Prefix + "/" + s.PrefixLength));
            _logger.Log(logLevel, "Used LAN subnets: {Subnets}", _lanSubnets.Where(s => !_excludedSubnets.Contains(s)).Select(s => s.Prefix + "/" + s.PrefixLength));
            _logger.Log(logLevel, "Filtered interface addresses: {Addresses}", _interfaces.OrderByDescending(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.Address));
            _logger.Log(logLevel, "Bind Addresses {Addresses}", GetAllBindInterfaces(false).OrderByDescending(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.Address));
            _logger.Log(logLevel, "Remote IP filter is {Type}", config.IsRemoteIPFilterBlacklist ? "Blocklist" : "Allowlist");
            _logger.Log(logLevel, "Filtered subnets: {Subnets}", _remoteAddressFilter.Select(s => s.Prefix + "/" + s.PrefixLength));
        }
    }
}
