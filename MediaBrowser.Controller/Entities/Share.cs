#pragma warning disable CS1591

namespace MediaBrowser.Controller.Entities
{
    public interface IHasShares
    {
        Share[] Shares { get; set; }
    }

    public class Share
    {
        public string UserId { get; set; }

        public bool CanEdit { get; set; }
    }
}
