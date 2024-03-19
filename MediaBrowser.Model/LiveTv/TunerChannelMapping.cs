#nullable disable

#pragma warning disable CS1591

namespace MediaBrowser.Model.LiveTv;

public class TunerChannelMapping
{
    public string Name { get; set; }

    public string ProviderChannelName { get; set; }

    public string ProviderChannelId { get; set; }

    public string Id { get; set; }
}
