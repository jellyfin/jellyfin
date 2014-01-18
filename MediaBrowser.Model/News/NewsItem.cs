using System;
using System.Collections.Generic;

namespace MediaBrowser.Model.News
{
    public class NewsChannel
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public List<NewsItem> Items { get; set; }
    }

    public class NewsItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string Guid { get; set; }
        public DateTime Date { get; set; }
    }

    public class NewsQuery
    {
        public int? StartIndex { get; set; }

        public int? Limit { get; set; }
    }
}
