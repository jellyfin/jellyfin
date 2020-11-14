namespace MediaBrowser.Model.SyncPlay
{
    /// <summary>
    /// Class NewGroupRequest.
    /// </summary>
    public class NewGroupRequest
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewGroupRequest"/> class.
        /// </summary>
        public NewGroupRequest()
        {
            GroupName = string.Empty;
        }

        /// <summary>
        /// Gets or sets the group name.
        /// </summary>
        /// <value>The name of the new group.</value>
        public string GroupName { get; set; }
    }
}
