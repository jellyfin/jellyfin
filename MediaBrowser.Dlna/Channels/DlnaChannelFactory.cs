namespace MediaBrowser.Dlna.Channels
{
    //public class DlnaChannelFactory : IChannelFactory, IDisposable
    //{
    //    private readonly IServerConfigurationManager _config;
    //    private readonly ILogger _logger;
    //    private readonly IHttpClient _httpClient;

    //    private readonly IDeviceDiscovery _deviceDiscovery;

    //    private readonly SemaphoreSlim _syncLock = new SemaphoreSlim(1, 1);
    //    private List<Device> _servers = new List<Device>();

    //    public static DlnaChannelFactory Instance;

    //    private Func<List<string>> _localServersLookup;

    //    public DlnaChannelFactory(IServerConfigurationManager config, IHttpClient httpClient, ILogger logger, IDeviceDiscovery deviceDiscovery)
    //    {
    //        _config = config;
    //        _httpClient = httpClient;
    //        _logger = logger;
    //        _deviceDiscovery = deviceDiscovery;
    //        Instance = this;
    //    }

    //    internal void Start(Func<List<string>> localServersLookup)
    //    {
    //        _localServersLookup = localServersLookup;

    //        //deviceDiscovery.DeviceDiscovered += deviceDiscovery_DeviceDiscovered;
    //        _deviceDiscovery.DeviceLeft += deviceDiscovery_DeviceLeft;
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

    //        if (GetExistingServers(usn).Any())
    //        {
    //            return;
    //        }

    //        await _syncLock.WaitAsync().ConfigureAwait(false);

    //        try
    //        {
    //            if (GetExistingServers(usn).Any())
    //            {
    //                return;
    //            }

    //            var device = await Device.CreateuPnpDeviceAsync(new Uri(location), _httpClient, _config, _logger)
    //                        .ConfigureAwait(false);

    //            if (!_servers.Any(i => string.Equals(i.Properties.UUID, device.Properties.UUID, StringComparison.OrdinalIgnoreCase)))
    //            {
    //                _servers.Add(device);
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

    //        if (!GetExistingServers(usn).Any())
    //        {
    //            return;
    //        }

    //        await _syncLock.WaitAsync().ConfigureAwait(false);

    //        try
    //        {
    //            var list = _servers.ToList();

    //            foreach (var device in GetExistingServers(usn).ToList())
    //            {
    //                list.Remove(device);
    //            }

    //            _servers = list;
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

    //    private IEnumerable<Device> GetExistingServers(string usn)
    //    {
    //        return _servers
    //            .Where(i => usn.IndexOf(i.Properties.UUID, StringComparison.OrdinalIgnoreCase) != -1);
    //    }

    //    public IEnumerable<IChannel> GetChannels()
    //    {
    //        //if (_servers.Count > 0)
    //        //{
    //        //    var service = _servers[0].Properties.Services
    //        //        .FirstOrDefault(i => string.Equals(i.ServiceType, "urn:schemas-upnp-org:service:ContentDirectory:1", StringComparison.OrdinalIgnoreCase));

    //        //    var controlUrl = service == null ? null : (_servers[0].Properties.BaseUrl.TrimEnd('/') + "/" + service.ControlUrl.TrimStart('/'));

    //        //    if (!string.IsNullOrEmpty(controlUrl))
    //        //    {
    //        //        return new List<IChannel>
    //        //    {
    //        //        new ServerChannel(_servers.ToList(), _httpClient, _logger, controlUrl)
    //        //    };
    //        //    }
    //        //}

    //        return new List<IChannel>();
    //    }

    //    public void Dispose()
    //    {
    //        if (_deviceDiscovery != null)
    //        {
    //            _deviceDiscovery.DeviceDiscovered -= deviceDiscovery_DeviceDiscovered;
    //            _deviceDiscovery.DeviceLeft -= deviceDiscovery_DeviceLeft;
    //        }
    //    }
    //}

    //public class ServerChannel : IChannel, IFactoryChannel
    //{
    //    private readonly IHttpClient _httpClient;
    //    private readonly ILogger _logger;
    //    public  string ControlUrl { get; set; }
    //    public List<Device> Servers { get; set; }

    //    public ServerChannel(IHttpClient httpClient, ILogger logger)
    //    {
    //        _httpClient = httpClient;
    //        _logger = logger;
    //        Servers = new List<Device>();
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
    //            {
    //                ChannelMediaContentType.Song,
    //                ChannelMediaContentType.Clip
    //            },

    //            MediaTypes = new List<ChannelMediaType>
    //            {
    //                ChannelMediaType.Audio,
    //                ChannelMediaType.Video,
    //                ChannelMediaType.Photo
    //            }
    //        };
    //    }

    //    public bool IsEnabledFor(string userId)
    //    {
    //        return true;
    //    }

    //    public async Task<ChannelItemResult> GetChannelItems(InternalChannelItemQuery query, CancellationToken cancellationToken)
    //    {
    //        IEnumerable<ChannelItemInfo> items;

    //        if (string.IsNullOrWhiteSpace(query.FolderId))
    //        {
    //            items = Servers.Select(i => new ChannelItemInfo
    //            {
    //                FolderType = ChannelFolderType.Container,
    //                Id = GetServerId(i),
    //                Name = i.Properties.Name,
    //                Overview = i.Properties.ModelDescription,
    //                Type = ChannelItemType.Folder
    //            });
    //        }
    //        else
    //        {
    //            var idParts = query.FolderId.Split('|');
    //            var folderId = idParts.Length == 2 ? idParts[1] : null;

    //            var result = await new ContentDirectoryBrowser(_httpClient, _logger).Browse(new ContentDirectoryBrowseRequest
    //            {
    //                Limit = query.Limit,
    //                StartIndex = query.StartIndex,
    //                ParentId = folderId,
    //                ContentDirectoryUrl = ControlUrl

    //            }, cancellationToken).ConfigureAwait(false);

    //            items = result.Items.ToList();
    //        }

    //        var list = items.ToList();
    //        var count = list.Count;

    //        list = ApplyPaging(list, query).ToList();

    //        return new ChannelItemResult
    //        {
    //            Items = list,
    //            TotalRecordCount = count
    //        };
    //    }

    //    private string GetServerId(Device device)
    //    {
    //        return device.Properties.UUID.GetMD5().ToString("N");
    //    }

    //    private IEnumerable<T> ApplyPaging<T>(IEnumerable<T> items, InternalChannelItemQuery query)
    //    {
    //        if (query.StartIndex.HasValue)
    //        {
    //            items = items.Skip(query.StartIndex.Value);
    //        }

    //        if (query.Limit.HasValue)
    //        {
    //            items = items.Take(query.Limit.Value);
    //        }

    //        return items;
    //    }

    //    public Task<DynamicImageResponse> GetChannelImage(ImageType type, CancellationToken cancellationToken)
    //    {
    //        // TODO: Implement
    //        return Task.FromResult(new DynamicImageResponse
    //        {
    //            HasImage = false
    //        });
    //    }

    //    public IEnumerable<ImageType> GetSupportedChannelImages()
    //    {
    //        return new List<ImageType>
    //        {
    //            ImageType.Primary
    //        };
    //    }
    //}
}
