using System;
using System.Collections.Generic;
using System.Linq;

namespace MediaBrowser.Controller.Entities.Movies
{
    public class Movie : Video
    {
        public IEnumerable<Video> SpecialFeatures { get; set; }

        /// <summary>
        /// Finds an item by ID, recursively
        /// </summary>
        public override BaseItem FindItemById(Guid id)
        {
            var item = base.FindItemById(id);

            if (item != null)
            {
                return item;
            }

            if (SpecialFeatures != null)
            {
                return SpecialFeatures.FirstOrDefault(i => i.Id == id);
            }

            return null;
        }
    }
}
