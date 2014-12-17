using MediaBrowser.Model.Dto;
using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public static class SyncHelper
    {
        public static List<SyncOptions> GetSyncOptions(List<BaseItemDto> items)
        {
            List<SyncOptions> options = new List<SyncOptions>();

            if (items.Count > 1)
            {
                options.Add(SyncOptions.Name);
            }
            
            foreach (BaseItemDto item in items)
            {
                if (item.SupportsSync ?? false)
                {
                    if (item.IsVideo)
                    {
                        options.Add(SyncOptions.Quality);
                        if (items.Count > 1)
                        {
                            options.Add(SyncOptions.UnwatchedOnly);
                        }
                        break;
                    }
                    if (item.IsFolder && !item.IsMusicGenre && !item.IsArtist && !item.IsType("musicalbum") && !item.IsGameGenre)
                    {
                        options.Add(SyncOptions.Quality);
                        options.Add(SyncOptions.UnwatchedOnly);
                        break;
                    }
                    if (item.IsGenre)
                    {
                        options.Add(SyncOptions.SyncNewContent);
                        options.Add(SyncOptions.ItemLimit);
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
                        options.Add(SyncOptions.SyncNewContent);
                        options.Add(SyncOptions.ItemLimit);
                        break;
                    }
                }
            }

            return options;
        }

        public static List<SyncOptions> GetSyncOptions(SyncCategory category)
        {
            List<SyncOptions> options = new List<SyncOptions>();

            options.Add(SyncOptions.Quality);
            options.Add(SyncOptions.UnwatchedOnly);
            options.Add(SyncOptions.SyncNewContent);
            options.Add(SyncOptions.ItemLimit);

            return options;
        }
    }
}
