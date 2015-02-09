using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public static class SyncHelper
    {
        public static List<SyncJobOption> GetSyncOptions(List<BaseItemDto> items)
        {
            List<SyncJobOption> options = new List<SyncJobOption>();

            if (items.Count > 1)
            {
                options.Add(SyncJobOption.Name);
            }
            
            foreach (BaseItemDto item in items)
            {
                if (item.SupportsSync ?? false)
                {
                    if (item.IsVideo)
                    {
                        options.Add(SyncJobOption.Quality);
                        if (items.Count > 1)
                        {
                            options.Add(SyncJobOption.UnwatchedOnly);
                        }
                        break;
                    }
                    if (item.IsFolder && !item.IsMusicGenre && !item.IsArtist && !item.IsType("musicalbum") && !item.IsGameGenre)
                    {
                        options.Add(SyncJobOption.Quality);
                        options.Add(SyncJobOption.UnwatchedOnly);
                        break;
                    }
                    if (item.IsGenre)
                    {
                        options.Add(SyncJobOption.SyncNewContent);
                        options.Add(SyncJobOption.ItemLimit);
                        break;
                    }
                }
            }

            foreach (BaseItemDto item in items)
            {
                if (item.SupportsSync ?? false)
                {
                    if (item.IsFolder || item.IsGameGenre || item.IsMusicGenre || item.IsGenre || item.IsArtist || item.IsStudio || item.IsPerson)
                    {
                        options.Add(SyncJobOption.SyncNewContent);
                        options.Add(SyncJobOption.ItemLimit);
                        break;
                    }
                }
            }

            return options;
        }

        public static List<SyncJobOption> GetSyncOptions(SyncCategory category)
        {
            List<SyncJobOption> options = new List<SyncJobOption>();

            options.Add(SyncJobOption.Name);
            options.Add(SyncJobOption.Quality);
            options.Add(SyncJobOption.UnwatchedOnly);
            options.Add(SyncJobOption.SyncNewContent);
            options.Add(SyncJobOption.ItemLimit);

            return options;
        }
    }
}
