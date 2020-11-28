namespace MediaBrowser.Model.SyncPlay.RequestBodies
{
    /// <summary>
    /// Class NewGroupRequestBody.
    /// </summary>
    public class NewGroupRequestBody
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NewGroupRequestBody"/> class.
        /// </summary>
        public NewGroupRequestBody()
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
