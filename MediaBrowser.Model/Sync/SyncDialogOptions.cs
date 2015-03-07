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
        /// <summary>
        /// Gets or sets the quality options.
        /// </summary>
        /// <value>The quality options.</value>
        public List<SyncQualityOption> QualityOptions { get; set; }
        
        public SyncDialogOptions()
        {
            Targets = new List<SyncTarget>();
            Options = new List<SyncJobOption>();
            QualityOptions = new List<SyncQualityOption>
            {
                new SyncQualityOption
                {
                    Name = SyncQuality.Original.ToString(),
                    Id = SyncQuality.Original.ToString()
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.High.ToString(),
                    Id = SyncQuality.High.ToString()
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.Medium.ToString(),
                    Id = SyncQuality.Medium.ToString()
                },
                new SyncQualityOption
                {
                    Name = SyncQuality.Low.ToString(),
                    Id = SyncQuality.Low.ToString()
                }
            };
        }
    }
}
