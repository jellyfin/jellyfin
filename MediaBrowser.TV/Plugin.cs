using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Entities;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Resolvers;
using System;

namespace MediaBrowser.TV
{
    public class Plugin : BasePlugin<BasePluginConfiguration>
    {
        protected override void InitInternal()
        {
            Kernel.Instance.AddBaseItemType<Series, SeriesResolver>();
            Kernel.Instance.AddBaseItemType<Season, SeasonResolver>();
            Kernel.Instance.AddBaseItemType<Episode, EpisodeResolver>();

            Kernel.Instance.ItemController.PreBeginResolvePath += ItemController_PreBeginResolvePath;
        }

        void ItemController_PreBeginResolvePath(object sender, PreBeginResolveEventArgs e)
        {
            if (e.IsFolder && System.IO.Path.GetFileName(e.Path).Equals("metadata", StringComparison.OrdinalIgnoreCase))
            {
                if (e.Parent is Season || e.Parent is Series)
                {
                    e.Cancel = true;
                }
            }
        }
    }
}
