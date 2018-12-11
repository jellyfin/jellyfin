using MediaBrowser.Common.Events;
using MediaBrowser.Common.Net;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Connect;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Net;
using MediaBrowser.Controller.Persistence;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Configuration;
using MediaBrowser.Model.Connect;
using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Events;
using MediaBrowser.Model.Logging;
using MediaBrowser.Model.Serialization;
using MediaBrowser.Model.Users;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Cryptography;
using MediaBrowser.Model.IO;
using MediaBrowser.Controller.Authentication;
using MediaBrowser.Controller.Security;
using MediaBrowser.Controller.Devices;
using MediaBrowser.Controller.Session;
using MediaBrowser.Controller.Plugins;

namespace Emby.Server.Implementations.Library
{
    public class ConnectManager : IConnectManager
    {
        public ConnectManager()
        {
        }

    }
}
