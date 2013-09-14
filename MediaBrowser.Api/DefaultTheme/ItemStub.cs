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
}
