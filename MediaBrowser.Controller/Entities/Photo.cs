using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class Photo : BaseItem, IHasTags, IHasTaglines
    {
        public List<string> Tags { get; set; }
        public List<string> Taglines { get; set; }

        public Photo()
        {
            Tags = new List<string>();
            Taglines = new List<string>();
        }

        public override string MediaType
        {
            get
            {
                return Model.Entities.MediaType.Photo;
            }
        }
    }
}
