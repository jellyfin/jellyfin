using System;

namespace MediaBrowser.Model.Social
{
    public class SocialShareInfo
    {
        public string Id { get; set; }
        public string Url { get; set; }
        public string ItemId { get; set; }
        public string UserId { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Name { get; set; }
        public string ImageUrl { get; set; }
        public string Overview { get; set; }
    }
}
