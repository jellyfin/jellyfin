#pragma warning disable CS1591

namespace MediaBrowser.Controller.Providers
{
    public interface IHasLookupInfo<out TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        TLookupInfoType GetLookupInfo();
    }
}
