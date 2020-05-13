#pragma warning disable CS1591

namespace Jellyfin.Data
{
    public interface ISavingChanges
    {
        void OnSavingChanges();
    }
}
