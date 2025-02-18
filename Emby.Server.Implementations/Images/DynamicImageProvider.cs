#nullable disable

#pragma warning disable CS1591

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Jellyfin.Data.Enums;
using Jellyfin.Extensions;
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

namespace Emby.Server.Implementations.Images;

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
        var recursive = isUsingCollectionStrip && view?.ViewType is not null && view.ViewType != CollectionType.boxsets && view.ViewType != CollectionType.playlists;

        var result = view.GetItemList(new InternalItemsQuery
        {
            User = view.UserId.HasValue ? _userManager.GetUserById(view.UserId.Value) : null,
            CollapseBoxSetItems = false,
            Recursive = recursive,
            ExcludeItemTypes = [BaseItemKind.UserView, BaseItemKind.CollectionFolder, BaseItemKind.Person],
            DtoOptions = new DtoOptions(false)
        });

        var items = result.Select(i =>
        {
            if (i is Episode episode)
            {
                var series = episode.Series;
                if (series is not null)
                {
                    return series;
                }

                return episode;
            }

            if (i is Season season)
            {
                var series = season.Series;
                if (series is not null)
                {
                    return series;
                }

                return season;
            }

            if (i is Audio audio)
            {
                var album = audio.AlbumEntity;
                if (album is not null && album.HasImage(ImageType.Primary))
                {
                    return album;
                }
            }

            return i;
        }).DistinctBy(x => x.Id);

        List<BaseItem> returnItems;
        if (isUsingCollectionStrip)
        {
            returnItems = items
                .Where(i => i.HasImage(ImageType.Primary) || i.HasImage(ImageType.Thumb))
                .ToList();
            returnItems.Shuffle();
            return returnItems;
        }

        returnItems = items
            .Where(i => i.HasImage(ImageType.Primary))
            .ToList();
        returnItems.Shuffle();
        return returnItems;
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
        CollectionType[] collectionStripViewTypes =
        [
            CollectionType.movies,
            CollectionType.tvshows,
            CollectionType.playlists
        ];

        return view?.ViewType is not null && collectionStripViewTypes.Contains(view.ViewType.Value);
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
