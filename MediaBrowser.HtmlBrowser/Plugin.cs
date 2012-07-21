using System;
using System.Reactive.Linq;
using MediaBrowser.Common.Net.Handlers;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.HtmlBrowser.Handlers;

namespace MediaBrowser.HtmlBrowser
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            var httpServer = Kernel.Instance.HttpServer;

            /*httpServer.Where(ctx => ctx.LocalPath.IndexOf("/browser/", StringComparison.OrdinalIgnoreCase) != -1).Subscribe(ctx =>
            {
                string localPath = ctx.LocalPath;
                string srch = "/browser/";

                int index = localPath.IndexOf(srch, StringComparison.OrdinalIgnoreCase);

                string resource = localPath.Substring(index + srch.Length);

                ctx.Respond(new EmbeddedResourceHandler(ctx, resource));

            });*/
        }
    }
}
