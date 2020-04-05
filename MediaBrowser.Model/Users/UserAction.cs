#nullable disable
#pragma warning disable CS1591

using System;

namespace MediaBrowser.Model.Users
{
    public class UserAction
    {
        public string Id { get; set; }
        public string ServerId { get; set; }
        public Guid UserId { get; set; }
        public Guid ItemId { get; set; }
        public UserActionType Type { get; set; }
        public DateTime Date { get; set; }
        public long? PositionTicks { get; set; }
    }
}
