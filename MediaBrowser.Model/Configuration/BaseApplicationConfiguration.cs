using MediaBrowser.Model.Updates;

namespace MediaBrowser.Model.Configuration
{
    /// <summary>
    /// Serves as a common base class for the Server and UI application Configurations
    /// ProtoInclude tells Protobuf about subclasses,
    /// The number 50 can be any number, so long as it doesn't clash with any of the ProtoMember numbers either here or in subclasses.
    /// </summary>
    public class BaseApplicationConfiguration
    {
        /// <summary>
        /// Gets or sets a value indicating whether [enable debug level logging].
        /// </summary>
        /// <value><c>true</c> if [enable debug level logging]; otherwise, <c>false</c>.</value>
        public bool EnableDebugLevelLogging { get; set; }

        /// <summary>
        /// Enable automatically and silently updating of the application
        /// </summary>
        /// <value><c>true</c> if [enable auto update]; otherwise, <c>false</c>.</value>
        public bool EnableAutoUpdate { get; set; }

        /// <summary>
        /// Gets of sets a value indicating the level of system updates (Release, Beta, Dev)
        /// </summary>
        public PackageVersionClass SystemUpdateLevel { get; set; }

        /// <summary>
        /// The number of days we should retain log files
        /// </summary>
        /// <value>The log file retention days.</value>
        public int LogFileRetentionDays { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [run at startup].
        /// </summary>
        /// <value><c>true</c> if [run at startup]; otherwise, <c>false</c>.</value>
        public bool RunAtStartup { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is first run.
        /// </summary>
        /// <value><c>true</c> if this instance is first run; otherwise, <c>false</c>.</value>
        public bool IsStartupWizardCompleted { get; set; }

        /// <summary>
        /// Gets or sets the cache path.
        /// </summary>
        /// <value>The cache path.</value>
        public string CachePath { get; set; }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationConfiguration" /> class.
        /// </summary>
        public BaseApplicationConfiguration()
        {
            EnableAutoUpdate = true;
            LogFileRetentionDays = 3;
        }
    }
}
