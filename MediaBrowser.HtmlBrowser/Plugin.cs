using System;
using System.Reactive.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;

namespace MediaBrowser.HtmlBrowser
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            var httpServer = Kernel.Instance.HttpServer;

            /*httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/browser/index.html", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new EmbeddedResourceHandler(ctx, "MediaBrowser.HtmlBrowser.Html.index.html")));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/browser/resource", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new EmbeddedResourceHandler(ctx)));

            httpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("/browser/favicon.ico", StringComparison.OrdinalIgnoreCase)).Subscribe(ctx => ctx.Respond(new EmbeddedResourceHandler(ctx, "MediaBrowser.HtmlBrowser.Html.css.images.favicon.ico")));*/
        }
    }
}
