using System.Text.Json.Serialization;

namespace MediaBrowser.Controller.Entities
{
    /// <summary>
    /// Plugins derive from and export this class to create a folder that will appear in the root along
    /// with all the other actual physical folders in the system.
    /// </summary>
    public abstract class BasePluginFolder : Folder, ICollectionFolder
    {
        [JsonIgnore]
        public virtual string CollectionType => null;

        public override bool CanDelete()
        {
            return false;
        }

        public override bool IsSaveLocalMetadataEnabled()
        {
            return true;
        }

        [JsonIgnore]
        public override bool SupportsInheritedParentImages => false;

        [JsonIgnore]
        public override bool SupportsPeople => false;

        //public override double? GetDefaultPrimaryImageAspectRatio()
        //{
        //    double value = 16;
        //    value /= 9;

        //    return value;
        //}
    }
}
