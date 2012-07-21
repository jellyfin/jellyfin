using System;
using System.Reactive.Linq;
using MediaBrowser.Api.HttpHandlers;
using MediaBrowser.Common.Net;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;

namespace MediaBrowser.Api
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            var httpServer = Kernel.Instance.HttpServer;

            httpServer.Where(ctx => ctx.LocalPath.IndexOf("/api/", StringComparison.OrdinalIgnoreCase) != -1).Subscribe(ctx =>
            {
                BaseHandler handler = GetHandler(ctx);

                if (handler != null)
                {
                    ctx.Respond(handler);
                }
            });
        }

        private BaseHandler GetHandler(RequestContext ctx)
        {
            BaseHandler handler = null;

            string localPath = ctx.LocalPath;

            if (localPath.EndsWith("/api/item", StringComparison.OrdinalIgnoreCase))
            {
                handler = new ItemHandler();
            }
            else if (localPath.EndsWith("/api/image", StringComparison.OrdinalIgnoreCase))
            {
                handler = new ImageHandler();
            } 
            else if (localPath.EndsWith("/api/users", StringComparison.OrdinalIgnoreCase))
            {
                handler = new UsersHandler();
            }
            else if (localPath.EndsWith("/api/media", StringComparison.OrdinalIgnoreCase))
            {
                handler = new MediaHandler();
            }
            else if (localPath.EndsWith("/api/genre", StringComparison.OrdinalIgnoreCase))
            {
                handler = new GenreHandler();
            }
            else if (localPath.EndsWith("/api/genres", StringComparison.OrdinalIgnoreCase))
            {
                handler = new GenresHandler();
            }
            else if (localPath.EndsWith("/api/studio", StringComparison.OrdinalIgnoreCase))
            {
                handler = new StudioHandler();
            }
            else if (localPath.EndsWith("/api/studios", StringComparison.OrdinalIgnoreCase))
            {
                handler = new StudiosHandler();
            }
            else if (localPath.EndsWith("/api/recentlyaddeditems", StringComparison.OrdinalIgnoreCase))
            {
                handler = new RecentlyAddedItemsHandler();
            }
            else if (localPath.EndsWith("/api/inprogressitems", StringComparison.OrdinalIgnoreCase))
            {
                handler = new InProgressItemsHandler();
            }

            if (handler != null)
            {
                handler.RequestContext = ctx;
            }

            return handler;
        }
    }
}
