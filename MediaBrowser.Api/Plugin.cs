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
            get { return "Media Browser API"; }
        }

        protected override void InitializeOnServer()
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

            if (IsUrlMatch("/api/item", localPath))
            {
                return new ItemHandler();
            }
            else if (IsUrlMatch("/api/image", localPath))
            {
                return new ImageHandler();
            }
            else if (IsUrlMatch("/api/users", localPath))
            {
                return new UsersHandler();
            }
            else if (IsUrlMatch("/api/itemlist", localPath))
            {
                return new ItemListHandler();
            }
            else if (IsUrlMatch("/api/genres", localPath))
            {
                return new GenresHandler();
            }
            else if (IsUrlMatch("/api/years", localPath))
            {
                return new YearsHandler();
            }
            else if (IsUrlMatch("/api/studios", localPath))
            {
                return new StudiosHandler();
            }
            else if (IsUrlMatch("/api/plugins", localPath))
            {
                return new PluginsHandler();
            }
            else if (IsUrlMatch("/api/pluginconfiguration", localPath))
            {
                return new PluginConfigurationHandler();
            }
            else if (IsUrlMatch("/api/static", localPath))
            {
                return new StaticFileHandler();
            }
            else if (IsUrlMatch("/api/audio", localPath))
            {
                return new AudioHandler();
            }
            else if (IsUrlMatch("/api/video", localPath))
            {
                return new VideoHandler();
            }
            else if (IsUrlMatch("/api/person", localPath))
            {
                return new PersonHandler();
            }
            else if (IsUrlMatch("/api/genre", localPath))
            {
                return new GenreHandler();
            }
            else if (IsUrlMatch("/api/year", localPath))
            {
                return new YearHandler();
            }
            else if (IsUrlMatch("/api/studio", localPath))
            {
                return new StudioHandler();
            }
            else if (IsUrlMatch("/api/weather", localPath))
            {
                return new WeatherHandler();
            }
            else if (IsUrlMatch("/api/serverconfiguration", localPath))
            {
                return new ServerConfigurationHandler();
            }
            else if (IsUrlMatch("/api/defaultuser", localPath))
            {
                return new DefaultUserHandler();
            }
            else if (IsUrlMatch("/api/pluginassembly", localPath))
            {
                return new PluginAssemblyHandler();
            }
            else if (IsUrlMatch("/api/UserAuthentication", localPath))
            {
                return new UserAuthenticationHandler();
            }

            return null;
        }

        private bool IsUrlMatch(string url, string localPath)
        {
            return localPath.EndsWith(url, StringComparison.OrdinalIgnoreCase);
        }
    }
}
