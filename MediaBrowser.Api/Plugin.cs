using System;
using System.ComponentModel.Composition;
using System.Net;
using System.Reactive.Linq;
using MediaBrowser.Api.HttpHandlers;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Model.Plugins;

namespace MediaBrowser.Api
{
    [Export(typeof(BasePlugin))]
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "WebAPI"; }
        }

        public override void Init()
        {
            var httpServer = Kernel.Instance.HttpServer;

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.IndexOf("/api/", StringComparison.OrdinalIgnoreCase) != -1).Subscribe((ctx) =>
            {
                BaseHandler handler = GetHandler(ctx);

                if (handler != null)
                {
                    handler.ProcessRequest(ctx);
                }
            });
        }

        private BaseHandler GetHandler(HttpListenerContext ctx)
        {
            string localPath = ctx.Request.Url.LocalPath;

            if (localPath.EndsWith("/api/item", StringComparison.OrdinalIgnoreCase))
            {
                return new ItemHandler();
            }
            else if (localPath.EndsWith("/api/image", StringComparison.OrdinalIgnoreCase))
            {
                return new ImageHandler();
            }
            else if (localPath.EndsWith("/api/users", StringComparison.OrdinalIgnoreCase))
            {
                return new UsersHandler();
            }
            else if (localPath.EndsWith("/api/itemlist", StringComparison.OrdinalIgnoreCase))
            {
                return new ItemListHandler();
            }
            else if (localPath.EndsWith("/api/genres", StringComparison.OrdinalIgnoreCase))
            {
                return new GenresHandler();
            }
            else if (localPath.EndsWith("/api/years", StringComparison.OrdinalIgnoreCase))
            {
                return new YearsHandler();
            }
            else if (localPath.EndsWith("/api/studios", StringComparison.OrdinalIgnoreCase))
            {
                return new StudiosHandler();
            }
            else if (localPath.EndsWith("/api/plugins", StringComparison.OrdinalIgnoreCase))
            {
                return new PluginsHandler();
            }
            else if (localPath.EndsWith("/api/pluginconfiguration", StringComparison.OrdinalIgnoreCase))
            {
                return new PluginConfigurationHandler();
            }
            else if (localPath.EndsWith("/api/static", StringComparison.OrdinalIgnoreCase))
            {
                return new StaticFileHandler();
            }
            else if (localPath.EndsWith("/api/audio", StringComparison.OrdinalIgnoreCase))
            {
                return new AudioHandler();
            }
            else if (localPath.EndsWith("/api/video", StringComparison.OrdinalIgnoreCase))
            {
                return new VideoHandler();
            }
            else if (localPath.EndsWith("/api/person", StringComparison.OrdinalIgnoreCase))
            {
                return new PersonHandler();
            }
            else if (localPath.EndsWith("/api/genre", StringComparison.OrdinalIgnoreCase))
            {
                return new GenreHandler();
            }
            else if (localPath.EndsWith("/api/year", StringComparison.OrdinalIgnoreCase))
            {
                return new YearHandler();
            }
            else if (localPath.EndsWith("/api/studio", StringComparison.OrdinalIgnoreCase))
            {
                return new StudioHandler();
            }
            else if (localPath.EndsWith("/api/weather", StringComparison.OrdinalIgnoreCase))
            {
                return new WeatherHandler();
            }

            return null;
        }
    }
}
