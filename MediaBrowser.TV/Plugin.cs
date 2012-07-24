using System;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Events;
using MediaBrowser.Model.Plugins;
using MediaBrowser.TV.Entities;
using MediaBrowser.TV.Resolvers;

namespace MediaBrowser.TV
{
    public class Plugin : BaseGenericPlugin<BasePluginConfiguration>
    {
        public override string Name
        {
            get { return "TV"; }
        }

        public override void InitInServer()
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
