#nullable disable

#pragma warning disable CA1819, CS1591

namespace MediaBrowser.Controller.Entities
{
    public interface IHasShares
    {
        Share[] Shares { get; set; }
    }
}
