#pragma warning disable CS1591

using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Emby.Dlna.Main;
using MediaBrowser.Common.Extensions;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Dlna;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.MediaEncoding;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Plugins;
using MediaBrowser.Controller.Session;
using MediaBrowser.Model.Dlna;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Globalization;
using MediaBrowser.Model.Services;
using MediaBrowser.Model.Session;
using Microsoft.Extensions.Logging;

namespace Emby.Dlna.PlayTo
{
    public class DlnaEventArgs // : IReturnVoid
    {
        public DlnaEventArgs(string id, string response)
        {
            Id = id;
            Response = response;
        }

        public string Id { get; }

        public string Response { get; }
    }
}
