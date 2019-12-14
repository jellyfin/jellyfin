#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Emby.Server.Implementations.Images;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Controller.Drawing;
using MediaBrowser.Controller.Dto;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Audio;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.IO;

namespace Emby.Server.Implementations.Images
{
    public class DynamicImageProvider : BaseDynamicImageProvider<UserView>
    {
        private readonly IUserManager _userManager;

        public DynamicImageProvider(IFileSystem fileSystem, IProviderManager providerManager, IApplicationPaths applicationPaths, IImageProcessor imageProcessor, IUserManager userManager)
            : base(fileSystem, providerManager, applicationPaths, imageProcessor)
        {
            _userManager = userManager;
        }

        protected override IReadOnlyList<BaseItem> GetItemsWithImages(BaseItem item)
        {
            var view = (UserView)item;

            var isUsingCollectionStrip = IsUsingCollectionStrip(view);
            var recursive = isUsingCollectionStrip && !new[] { CollectionType.BoxSets, CollectionType.Playlists }.Contains(view.ViewType ?? string.Empty, StringComparer.OrdinalIgnoreCase);

            var result = view.GetItemList(new InternalItemsQuery
            {
                User = view.UserId.HasValue ? _userManager.GetUserById(view.UserId.Value) : null,
                CollapseBoxSetItems = false,
                Recursive = recursive,
                ExcludeItemTypes = new[] { "UserView", "CollectionFolder", "Person" },
                DtoOptions = new DtoOptions(false)
            });

            var items = result.Select(i =>
            {
                if (i is Episode episode)
                {
                    var series = episode.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return episode;
                }

                if (i is Season season)
                {
                    var series = season.Series;
                    if (series != null)
                    {
                        return series;
                    }

                    return season;
                }

                if (i is Audio audio)
                {
                    var album = audio.AlbumEntity;
                    if (album != null && album.HasImage(ImageType.Primary))
                    {
                        return album;
                    }
                }

                return i;

            }).GroupBy(x => x.Id)
            .Select(x => x.First());

            if (isUsingCollectionStrip)
            {
                return items
                    .Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb))
                    .ToList();
            }

            return items
                .Where(i => i.HasImage(ImageType.Primary))
                .ToList();
        }

        protected override bool Supports(BaseItem item)
        {
            if (item is UserView view)
            {
                return IsUsingCollectionStrip(view);
            }

            return false;
        }

        private static bool IsUsingCollectionStrip(UserView view)
        {
            string[] collectionStripViewTypes =
            {
                CollectionType.Movies,
                CollectionType.TvShows,
                CollectionType.Playlists
            };

            return collectionStripViewTypes.Contains(view.ViewType ?? string.Empty);
        }

        protected override string CreateImage(BaseItem item, IReadOnlyCollection<BaseItem> itemsWithImages, string outputPathWithoutExtension, ImageType imageType, int imageIndex)
        {
            if (itemsWithImages.Count == 0)
            {
                return null;
            }

            var outputPath = Path.ChangeExtension(outputPathWithoutExtension, ".png");

            return CreateThumbCollage(item, itemsWithImages, outputPath, 960, 540);
        }
    }
}
