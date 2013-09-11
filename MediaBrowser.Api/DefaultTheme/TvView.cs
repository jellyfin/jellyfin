using MediaBrowser.Model.Dto;

namespace MediaBrowser.Api.DefaultTheme
{
    public class TvView
    {
        public BaseItemDto[] SpotlightItems { get; set; }
        public ItemStub[] ShowsItems { get; set; }
        public ItemStub[] ActorItems { get; set; }
    }
}
