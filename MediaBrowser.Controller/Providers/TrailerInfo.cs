using MediaBrowser.Model.Entities;

namespace MediaBrowser.Controller.Providers
{
    public class TrailerInfo : ItemLookupInfo
    {
        public bool IsLocalTrailer { get; set; }
    }
}