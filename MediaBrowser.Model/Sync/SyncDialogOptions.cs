using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncDialogOptions
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        /// <value>The targets.</value>
        public SyncTarget[] Targets { get; set; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public SyncJobOption[] Options { get; set; }
        /// <summary>
        /// Gets or sets the quality options.
        /// </summary>
        /// <value>The quality options.</value>
        public SyncQualityOption[] QualityOptions { get; set; }
        /// <summary>
        /// Gets or sets the profile options.
        /// </summary>
        /// <value>The profile options.</value>
        public SyncProfileOption[] ProfileOptions { get; set; }
     
        public SyncDialogOptions()
        {
            Targets = new SyncTarget[] { };
            Options = new SyncJobOption[] { };
            QualityOptions = new SyncQualityOption[] { };
            ProfileOptions = new SyncProfileOption[] { };
        }
    }
}
