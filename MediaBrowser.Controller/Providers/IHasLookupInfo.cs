namespace MediaBrowser.Controller.Providers
{
    /// <summary>
    /// Interface for items that provide lookup info for metadata providers.
    /// </summary>
    /// <typeparam name="TLookupInfoType">The type of the lookup info.</typeparam>
    public interface IHasLookupInfo<out TLookupInfoType>
        where TLookupInfoType : ItemLookupInfo, new()
    {
        /// <summary>
        /// Gets the lookup info.
        /// </summary>
        /// <returns>The lookup info.</returns>
        TLookupInfoType GetLookupInfo();
    }
}
