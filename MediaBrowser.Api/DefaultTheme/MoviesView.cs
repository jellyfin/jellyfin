
namespace MediaBrowser.Api.DefaultTheme
{
    public class MoviesView
    {
        public ItemStub[] SpotlightItems { get; set; }
        public ItemStub[] BackdropItems { get; set; }
        public ItemStub[] MovieItems { get; set; }
        public ItemStub[] PeopleItems { get; set; }

        public ItemStub[] BoxSetItems { get; set; }
        public ItemStub[] TrailerItems { get; set; }
        public ItemStub[] HDItems { get; set; }
        public ItemStub[] ThreeDItems { get; set; }
    }
}
