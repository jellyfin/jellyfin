using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using MediaBrowser.Api.HttpHandlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;

namespace MediaBrowser.Api
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            var httpServer = Kernel.Instance.HttpServer;

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/item", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new ItemHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/image", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new ImageHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/genre", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new GenreHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/genres", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new GenresHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/recentlyaddeditems", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new RecentlyAddedItemsHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/api/inprogressitems", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new InProgressItemsHandler(ctx)));
        }
    }
}
