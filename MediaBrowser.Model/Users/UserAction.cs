using System;

namespace MediaBrowser.Model.Users
{
    public class UserAction
    {
        public string Id { get; set; }
        public string ServerId { get; set; }
        public string UserId { get; set; }
        public string ItemId { get; set; }
        public UserActionType Type { get; set; }
        public DateTime Date { get; set; }
        public long? PositionTicks { get; set; }
    }
}
