namespace MediaBrowser.Model.Updates
{
    /// <summary>
    /// Class InstallationInfo
    /// </summary>
    public class InstallationInfo
    {
        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        /// <value>The id.</value>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the assembly guid.
        /// </summary>
        /// <value>The guid of the assembly.</value>
        public string AssemblyGuid { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
        public string Version { get; set; }

        /// <summary>
        /// Gets or sets the update class.
        /// </summary>
        /// <value>The update class.</value>
        public PackageVersionClass UpdateClass { get; set; }

        /// <summary>
        /// Gets or sets the percent complete.
        /// </summary>
        /// <value>The percent complete.</value>
        public double? PercentComplete { get; set; }
    }
}
