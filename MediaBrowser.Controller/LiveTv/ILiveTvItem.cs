using MediaBrowser.Controller.Entities;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvItem : IHasId
    {
        string ServiceName { get; set; }
    }
}
