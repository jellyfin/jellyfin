using System;

namespace MediaBrowser.Controller.LiveTv
{
    public interface ILiveTvItem
    {
        Guid Id { get; }
        string ServiceName { get; set; }
    }
}
