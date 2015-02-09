using System.Collections.Generic;

namespace MediaBrowser.Model.Sync
{
    public class SyncDialogOptions
    {
        /// <summary>
        /// Gets or sets the targets.
        /// </summary>
        /// <value>The targets.</value>
        public List<SyncTarget> Targets { get; set; }
        /// <summary>
        /// Gets or sets the options.
        /// </summary>
        /// <value>The options.</value>
        public List<SyncJobOption> Options { get; set; }

        public SyncDialogOptions()
        {
            Targets = new List<SyncTarget>();
            Options = new List<SyncJobOption>();
        }
    }
}
