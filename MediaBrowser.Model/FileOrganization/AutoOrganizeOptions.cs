
namespace MediaBrowser.Model.FileOrganization
{
    public class AutoOrganizeOptions
    {
        /// <summary>
        /// Gets or sets the tv options.
        /// </summary>
        /// <value>The tv options.</value>
        public TvFileOrganizationOptions TvOptions { get; set; }

        /// <summary>
        /// Gets or sets a list of smart match entries.
        /// </summary>
        /// <value>The smart match entries.</value>
        public SmartMatchInfo[] SmartMatchInfos { get; set; }

        public AutoOrganizeOptions()
        {
            TvOptions = new TvFileOrganizationOptions();
            SmartMatchInfos = new SmartMatchInfo[]{};
        }
    }
}
