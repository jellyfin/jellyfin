using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;
using System.Collections.Generic;

namespace MediaBrowser.Api.DefaultTheme
{
    public class ItemStub
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public Guid ImageTag { get; set; }
        public ImageType ImageType { get; set; }
    }

    public class MoviesView : BaseView
    {
        public List<ItemStub> MovieItems { get; set; }
        public List<ItemStub> PeopleItems { get; set; }

        public List<ItemStub> BoxSetItems { get; set; }
        public List<ItemStub> TrailerItems { get; set; }
        public List<ItemStub> HDItems { get; set; }
        public List<ItemStub> ThreeDItems { get; set; }

        public List<ItemStub> FamilyMovies { get; set; }

        public List<ItemStub> RomanceItems { get; set; }
        public List<ItemStub> ComedyItems { get; set; }

        public double FamilyMoviePercentage { get; set; }

        public double HDMoviePercentage { get; set; }
    }

    public class TvView : BaseView
    {
        public List<ItemStub> ShowsItems { get; set; }
        public List<ItemStub> ActorItems { get; set; }

        public List<ItemStub> RomanceItems { get; set; }
        public List<ItemStub> ComedyItems { get; set; }

        public int SeriesCount { get; set; }
        public int FavoriteSeriesCount { get; set; }
        public int TopCommunityRatedSeriesCount { get; set; }
        public int InProgressSeriesCount { get; set; }
    }

    public class GamesView : BaseView
    {
        public List<ItemStub> MultiPlayerItems { get; set; }

    }

    public class BaseView
    {
        public List<BaseItemDto> BackdropItems { get; set; }
        public List<BaseItemDto> SpotlightItems { get; set; }
        public List<BaseItemDto> MiniSpotlights { get; set; }
    }

    public class FavoritesView : BaseView
    {
        public List<BaseItemDto> Movies { get; set; }
        public List<BaseItemDto> Series { get; set; }
        public List<BaseItemDto> Episodes { get; set; }
        public List<BaseItemDto> Games { get; set; }
        public List<BaseItemDto> Books { get; set; }
        public List<BaseItemDto> Albums { get; set; }
        public List<BaseItemDto> Songs { get; set; }
        public List<BaseItemDto> Artists { get; set; }
    }
}
