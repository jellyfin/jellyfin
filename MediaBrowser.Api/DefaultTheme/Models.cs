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

    public class MoviesView
    {
        public List<BaseItemDto> SpotlightItems { get; set; }
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

    public class TvView
    {
        public List<BaseItemDto> SpotlightItems { get; set; }
        public List<ItemStub> ShowsItems { get; set; }
        public List<ItemStub> ActorItems { get; set; }

        public List<ItemStub> RomanceItems { get; set; }
        public List<ItemStub> ComedyItems { get; set; }
    }

    public class GamesView
    {
        public List<BaseItemDto> SpotlightItems { get; set; }
    }

    public class HomeView
    {
        public List<BaseItemDto> SpotlightItems { get; set; }
    }
}
