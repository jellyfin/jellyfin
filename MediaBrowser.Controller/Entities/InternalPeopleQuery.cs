using System;
using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public class InternalPeopleQuery
    {
        public Guid ItemId { get; set; }
        public List<string> PersonTypes { get; set; }
        public List<string> ExcludePersonTypes { get; set; }
        public int? MaxListOrder { get; set; }
        public Guid AppearsInItemId { get; set; }
        public string NameContains { get; set; }
        public SourceType[] SourceTypes { get; set; }

        public InternalPeopleQuery()
        {
            PersonTypes = new List<string>();
            ExcludePersonTypes = new List<string>();
            SourceTypes = new SourceType[] { };
        }
    }
}
