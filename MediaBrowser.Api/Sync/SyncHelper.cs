using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Sync;
using System.Collections.Generic;

namespace MediaBrowser.Api.Sync
{
    public static class SyncHelper
    {
        public static List<SyncJobOption> GetSyncOptions(List<BaseItemDto> items)
        {
            List<SyncJobOption> options = new List<SyncJobOption>();

            foreach (BaseItemDto item in items)
            {
                if (item.SupportsSync ?? false)
                {
                    if (item.IsVideo)
                    {
                        options.Add(SyncJobOption.Quality);
                        options.Add(SyncJobOption.Profile);
                        if (items.Count > 1)
                        {
                            options.Add(SyncJobOption.UnwatchedOnly);
                        }
                        break;
                    }
                    if (item.IsAudio)
                    {
                        options.Add(SyncJobOption.Quality);
                        options.Add(SyncJobOption.Profile);
                        break;
                    }
                    if (item.IsMusicGenre || item.IsArtist|| item.IsType("musicalbum"))
                    {
                        options.Add(SyncJobOption.Quality);
                        options.Add(SyncJobOption.Profile);
                        options.Add(SyncJobOption.ItemLimit);
                        break;
                    }
                    if (item.IsFolderItem && !item.IsMusicGenre && !item.IsArtist && !item.IsType("musicalbum") && !item.IsGameGenre)
                    {
                        options.Add(SyncJobOption.Quality);
                        options.Add(SyncJobOption.Profile);
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
                    if (item.IsFolderItem || item.IsGameGenre || item.IsMusicGenre || item.IsGenre || item.IsArtist || item.IsStudio || item.IsPerson)
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

            options.Add(SyncJobOption.Quality);
            options.Add(SyncJobOption.Profile);
            options.Add(SyncJobOption.UnwatchedOnly);
            options.Add(SyncJobOption.SyncNewContent);
            options.Add(SyncJobOption.ItemLimit);

            return options;
        }
    }
}
