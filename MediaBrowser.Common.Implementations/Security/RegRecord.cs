using System;

namespace MediaBrowser.Common.Implementations.Security
{
    class RegRecord
    {
        public string featId { get; set; }
        public bool registered { get; set; }
        public DateTime expDate { get; set; }
        public string key { get; set; }
    }
}