using System.Collections.Generic;

namespace MediaBrowser.Controller.Entities
{
    public interface IHasShares
    {
        List<Share> Shares { get; set; }
    }

    public class Share
    {
        public string UserId { get; set; }
        public bool CanEdit { get; set; }
    }
}
