namespace MediaBrowser.Dlna.Channels
{
    //public class DlnaChannel : IChannel, IDisposable
    //{
    //    private readonly ILogger _logger;
    //    private readonly IHttpClient _httpClient;
    //    private readonly IServerConfigurationManager _config;
    //    private List<Device> _servers = new List<Device>();

    //    private readonly IDeviceDiscovery _deviceDiscovery;
    //    private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
    //    private Func<List<string>> _localServersLookup;

    //    public static DlnaChannel Current;

    //    public DlnaChannel(ILogger logger, IHttpClient httpClient, IDeviceDiscovery deviceDiscovery, IServerConfigurationManager config)
    //    {
    //        _logger = logger;
    //        _httpClient = httpClient;
    //        _deviceDiscovery = deviceDiscovery;
    //        _config = config;
    //        Current = this;
    //    }

    //    public string Name
    //    {
    //        get { return "Devices"; }
    //    }

    //    public string Description
    //    {
    //        get { return string.Empty; }
    //    }

    //    public string DataVersion
    //    {
    //        get { return DateTime.UtcNow.Ticks.ToString(); }
    //    }

    //    public string HomePageUrl
    //    {
    //        get { return string.Empty; }
    //    }

    //    public ChannelParentalRating ParentalRating
    //    {
    //        get { return ChannelParentalRating.GeneralAudience; }
    //    }

    //    public InternalChannelFeatures GetChannelFeatures()
    //    {
    //        return new InternalChannelFeatures
    //        {
    //            ContentTypes = new List<ChannelMediaContentType>
    //                {
    //                    ChannelMediaContentType.Song,
    //                    ChannelMediaContentType.Clip
    //                },

    //            MediaTypes = new List<ChannelMediaType>
    //                {
    //                    ChannelMediaType.Audio,
    //                    ChannelMediaType.Video,
    //                    ChannelMediaType.Photo
    //                }
    //        };
    //    }

    //    public bool IsEnabledFor(string userId)
    //    {
    //        return true;
    //    }

    //    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    //    {
    //        throw new NotImplementedException();
    //    }

    //    public IEnumerable<ImageType> GetSupportedChannelImages()
    //    {
    //        return new List<ImageType>
    //            {
    //                ImageType.Primary
    //            };
    //    }

    //    public void Start(Func<List<string>> localServersLookup)
    //    {
    //        _localServersLookup = localServersLookup;

    //        _deviceDiscovery.DeviceDiscovered -= deviceDiscovery_DeviceDiscovered;
    //        _deviceDiscovery.DeviceLeft -= deviceDiscovery_DeviceLeft;

    //        _deviceDiscovery.DeviceDiscovered += deviceDiscovery_DeviceDiscovered;
    //        _deviceDiscovery.DeviceLeft += deviceDiscovery_DeviceLeft;
    //    }

    //    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    //    {
    //        if (string.IsNullOrWhiteSpace(query.FolderId))
    //        {
    //            return await GetServers(query, cancellationToken).ConfigureAwait(false);
    //        }

    //        return new ChannelItemResult();

    //        //var idParts = query.FolderId.Split('|');
    //        //var folderId = idParts.Length == 2 ? idParts[1] : null;

    //        //var result = await new ContentDirectoryBrowser(_httpClient, _logger).Browse(new ContentDirectoryBrowseRequest
    //        //{
    //        //    Limit = query.Limit,
    //        //    StartIndex = query.StartIndex,
    //        //    ParentId = folderId,
    //        //    ContentDirectoryUrl = ControlUrl

    //        //}, cancellationToken).ConfigureAwait(false);

    //        //items = result.Items.ToList();

    //        //var list = items.ToList();
    //        //var count = list.Count;

    //        //list = ApplyPaging(list, query).ToList();

    //        //return new ChannelItemResult
    //        //{
    //        //    Items = list,
    //        //    TotalRecordCount = count
    //        //};
    //    }

    //    public async Task<ChannelItemResult> GetServers(InternalChannelItemQuery query, CancellationToken cancellationToken)
    //    {
    //        await _syncLock.WaitAsync(cancellationToken).ConfigureAwait(false);

    //        try
    //        {
    //            var items = _servers.Select(i =>
    //            {
    //                var service = i.Properties.Services
    //                .FirstOrDefault(s => string.Equals(s.ServiceType, "urn:schemas-upnp-org:service:ContentDirectory:1", StringComparison.OrdinalIgnoreCase));

    //                var controlUrl = service == null ? null : (_servers[0].Properties.BaseUrl.TrimEnd('/') + "/" + service.ControlUrl.TrimStart('/'));

    //                if (string.IsNullOrWhiteSpace(controlUrl))
    //                {
    //                    return null;
    //                }

    //                return new ChannelItemInfo
    //                {
    //                    Id = i.Properties.UUID,
    //                    Name = i.Properties.Name,
    //                    Type = ChannelItemType.Folder
    //                };

    //            }).Where(i => i != null).ToList();

    //            return new ChannelItemResult
    //            {
    //                TotalRecordCount = items.Count,
    //                Items = items
    //            };
    //        }
    //        finally
    //        {
    //            _syncLock.Release();
    //        }
    //    }

    //    async void deviceDiscovery_DeviceDiscovered(object sender, SsdpMessageEventArgs e)
    //    {
    //        string usn;
    //        if (!e.Headers.TryGetValue("USN", out usn)) usn = string.Empty;

    //        string nt;
    //        if (!e.Headers.TryGetValue("NT", out nt)) nt = string.Empty;

    //        string location;
    //        if (!e.Headers.TryGetValue("Location", out location)) location = string.Empty;

    //        if (!IsValid(nt, usn))
    //        {
    //            return;
    //        }

    //        if (_localServersLookup != null)
    //        {
    //            if (_localServersLookup().Any(i => usn.IndexOf(i, StringComparison.OrdinalIgnoreCase) != -1))
    //            {
    //                // Don't add the local Dlna server to this
    //                return;
    //            }
    //        }

    //        await _syncLock.WaitAsync().ConfigureAwait(false);

    //        var serverList = _servers.ToList();

    //        try
    //        {
    //            if (GetExistingServers(serverList, usn).Any())
    //            {
    //                return;
    //            }

    //            var device = await Device.CreateuPnpDeviceAsync(new Uri(location), _httpClient, _config, _logger)
    //                        .ConfigureAwait(false);

    //            if (!serverList.Any(i => string.Equals(i.Properties.UUID, device.Properties.UUID, StringComparison.OrdinalIgnoreCase)))
    //            {
    //                serverList.Add(device);
    //            }
    //        }
    //        catch (Exception ex)
    //        {

    //        }
    //        finally
    //        {
    //            _syncLock.Release();
    //        }
    //    }

    //    async void deviceDiscovery_DeviceLeft(object sender, SsdpMessageEventArgs e)
    //    {
    //        string usn;
    //        if (!e.Headers.TryGetValue("USN", out usn)) usn = String.Empty;

    //        string nt;
    //        if (!e.Headers.TryGetValue("NT", out nt)) nt = String.Empty;

    //        if (!IsValid(nt, usn))
    //        {
    //            return;
    //        }

    //        await _syncLock.WaitAsync().ConfigureAwait(false);

    //        try
    //        {
    //            var serverList = _servers.ToList();

    //            var matchingServers = GetExistingServers(serverList, usn);
    //            if (matchingServers.Count > 0)
    //            {
    //                foreach (var device in matchingServers)
    //                {
    //                    serverList.Remove(device);
    //                }

    //                _servers = serverList;
    //            }
    //        }
    //        finally
    //        {
    //            _syncLock.Release();
    //        }
    //    }

    //    private bool IsValid(string nt, string usn)
    //    {
    //        // It has to report that it's a media renderer
    //        if (usn.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1 &&
    //                 nt.IndexOf("ContentDirectory:", StringComparison.OrdinalIgnoreCase) == -1 &&
    //                 usn.IndexOf("MediaServer:", StringComparison.OrdinalIgnoreCase) == -1 &&
    //                 nt.IndexOf("MediaServer:", StringComparison.OrdinalIgnoreCase) == -1)
    //        {
    //            return false;
    //        }

    //        return true;
    //    }

    //    private List<Device> GetExistingServers(List<Device> allDevices, string usn)
    //    {
    //        return allDevices
    //            .Where(i => usn.IndexOf(i.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1)
    //            .ToList();
    //    }

    //    public void Dispose()
    //    {
    //        _deviceDiscovery.DeviceDiscovered -= deviceDiscovery_DeviceDiscovered;
    //        _deviceDiscovery.DeviceLeft -= deviceDiscovery_DeviceLeft;
    //    }
    //}
}
