
namespace MediaBrowser.Model.FileOrganization
{
    public class AutoOrganizeOptions
    {
        /// <summary>
        /// Gets or sets the tv options.
        /// </summary>
        /// <value>The tv options.</value>
        public TvFileOrganizationOptions TvOptions { get; set; }

        public AutoOrganizeOptions()
        {
            TvOptions = new TvFileOrganizationOptions();
        }
    }
}
