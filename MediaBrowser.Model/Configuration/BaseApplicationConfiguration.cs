#nullable disable
using System;
using System.Xml.Serialization;

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
        /// The number of days we should retain log files
        /// </summary>
        /// <value>The log file retention days.</value>
        public int LogFileRetentionDays { get; set; }

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
        /// Last known version that was ran using the configuration.
        /// </summary>
        /// <value>The version from previous run.</value>
        [XmlIgnore]
        public Version PreviousVersion { get; set; }

        /// <summary>
        /// Stringified PreviousVersion to be stored/loaded,
        /// because System.Version itself isn't xml-serializable
        /// </summary>
        /// <value>String value of PreviousVersion</value>
        public string PreviousVersionStr
        {
            get => PreviousVersion?.ToString();
            set => PreviousVersion = Version.Parse(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseApplicationConfiguration" /> class.
        /// </summary>
        public BaseApplicationConfiguration()
        {
            LogFileRetentionDays = 3;
        }
    }
}
