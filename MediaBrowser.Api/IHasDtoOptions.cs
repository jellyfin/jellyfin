
namespace MediaBrowser.Api
{
    public interface IHasDtoOptions : IHasItemFields
    {
        bool? EnableImages { get; set; }
        bool? EnableUserData { get; set; }

        int? ImageTypeLimit { get; set; }

        string EnableImageTypes { get; set; }
    }
}
