
namespace MediaBrowser.Model.Library
{
    public class UserViewQuery
    {
        /// <summary>
        /// Gets or sets the user identifier.
        /// </summary>
        /// <value>The user identifier.</value>
        public string UserId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include external content].
        /// </summary>
        /// <value><c>true</c> if [include external content]; otherwise, <c>false</c>.</value>
        public bool IncludeExternalContent { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [include hidden].
        /// </summary>
        /// <value><c>true</c> if [include hidden]; otherwise, <c>false</c>.</value>
        public bool IncludeHidden { get; set; }

        public string[] PresetViews { get; set; }

        public UserViewQuery()
        {
            IncludeExternalContent = true;
            PresetViews = new string[] { };
        }
    }
}
