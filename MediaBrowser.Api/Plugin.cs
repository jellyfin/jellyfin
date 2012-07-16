using System;
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

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/users", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new UsersHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/media", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new MediaHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/item", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new ItemHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/image", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new ImageHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/genre", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new GenreHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/genres", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new GenresHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/studio", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new StudioHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/studios", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new StudiosHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/recentlyaddeditems", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new RecentlyAddedItemsHandler(ctx)));

            httpServer.Where(ctx => ctx.LocalPath.EndsWith("/api/inprogressitems", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new InProgressItemsHandler(ctx)));
        }
    }
}
