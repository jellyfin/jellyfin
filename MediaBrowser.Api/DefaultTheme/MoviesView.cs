using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api.DefaultTheme
{
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

        public ItemStub[] RomanticItems { get; set; }

        public double FamilyMoviePercentage { get; set; }

        public double HDMoviePercentage { get; set; }
    }
}
