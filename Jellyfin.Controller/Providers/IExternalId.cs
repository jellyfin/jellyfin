using Jellyfin.Model.Entities;

namespace Jellyfin.Controller.Providers
{
    public interface IExternalId
    {
        string Name { get; }

        string Key { get; }

        string UrlFormatString { get; }

        bool Supports(IHasProviderIds item);
    }
}
