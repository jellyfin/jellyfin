using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Net;
using MediaBrowser.Api.HttpHandlers;

namespace MediaBrowser.Api
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        List<IDisposable> HttpHandlers = new List<IDisposable>();

        protected override void InitInternal()
        {
            HttpHandlers.Add(Kernel.Instance.HttpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("mediabrowser/api/item")).Subscribe(ctx => ctx.Respond(new ItemHandler(ctx))));
            HttpHandlers.Add(Kernel.Instance.HttpServer.Where(ctx => ctx.Request.Url.LocalPath.EndsWith("mediabrowser/api/image")).Subscribe(ctx => ctx.Respond(new ItemHandler(ctx))));
        }

        public override void Dispose()
        {
            base.Dispose();

            foreach (var handler in HttpHandlers)
            {
                handler.Dispose();
            }

            HttpHandlers.Clear();
        }
    }
}
