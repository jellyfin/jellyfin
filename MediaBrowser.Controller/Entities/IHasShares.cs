#nullable disable

#pragma warning disable CS1591

namespace MediaBrowser.Controller.Entities
{
    public interface IHasShares
    {
        Share[] Shares { get; set; }
    }
}