using System;

namespace MediaBrowser.Controller.Entities
{
    public class InternalPeopleQuery
    {
        public int Limit { get; set; }

        public Guid ItemId { get; set; }

        public string[] PersonTypes { get; set; }

        public string[] ExcludePersonTypes { get; set; }

        public int? MaxListOrder { get; set; }

        public Guid AppearsInItemId { get; set; }

        public string NameContains { get; set; }

        public InternalPeopleQuery()
        {
            PersonTypes = Array.Empty<string>();
            ExcludePersonTypes = Array.Empty<string>();
        }
    }
}
