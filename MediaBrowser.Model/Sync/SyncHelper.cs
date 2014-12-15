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
                        options.Add(SyncOptions.UnwatchedOnly);
                        break;
                    }
                    if (item.IsFolder && !item.IsMusicGenre && !item.IsArtist && !item.IsType("musicalbum") && !item.IsGameGenre)
                    {
                        options.Add(SyncOptions.Quality);
                        options.Add(SyncOptions.UnwatchedOnly);
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
                        break;
                    }
                }
            }

            return options;
        }
    }
}
