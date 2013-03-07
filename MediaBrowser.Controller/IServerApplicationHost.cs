using MediaBrowser.Common;
using MediaBrowser.Model.System;

namespace MediaBrowser.Controller
{
    public interface IServerApplicationHost : IApplicationHost
    {
        SystemInfo GetSystemInfo();
    }
}
