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
}