using MediaBrowser.Model.Dto;
using MediaBrowser.Model.Entities;
using System;

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
        public BaseItemDto[] SpotlightItems { get; set; }
        public ItemStub[] MovieItems { get; set; }
        public ItemStub[] PeopleItems { get; set; }

        public ItemStub[] BoxSetItems { get; set; }
        public ItemStub[] TrailerItems { get; set; }
        public ItemStub[] HDItems { get; set; }
        public ItemStub[] ThreeDItems { get; set; }

        public ItemStub[] FamilyMovies { get; set; }

        public ItemStub[] RomanceItems { get; set; }
        public ItemStub[] ComedyItems { get; set; }

        public double FamilyMoviePercentage { get; set; }

        public double HDMoviePercentage { get; set; }
    }

    public class TvView
    {
        public BaseItemDto[] SpotlightItems { get; set; }
        public ItemStub[] ShowsItems { get; set; }
        public ItemStub[] ActorItems { get; set; }

        public ItemStub[] RomanceItems { get; set; }
        public ItemStub[] ComedyItems { get; set; }
    }

    public class HomeView
    {
        public BaseItemDto[] SpotlightItems { get; set; }
    }
}
