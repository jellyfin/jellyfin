using System;

namespace MediaBrowser.Model.News
{
    public class NewsItem
    {
        public string Title { get; set; }
        public string Link { get; set; }
        public string Description { get; set; }
        public string DescriptionHtml { get; set; }
        public string Guid { get; set; }
        public DateTime Date { get; set; }
    }
}
