using System.Collections.Generic;
using MediaBrowser.Model.Dto;

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
        public List<NameValuePair> QualityOptions { get; set; }
        
        public SyncDialogOptions()
        {
            Targets = new List<SyncTarget>();
            Options = new List<SyncJobOption>();
            QualityOptions = new List<NameValuePair>
            {
                new NameValuePair
                {
                    Name = SyncQuality.Original.ToString(),
                    Value = SyncQuality.Original.ToString()
                },
                new NameValuePair
                {
                    Name = SyncQuality.High.ToString(),
                    Value = SyncQuality.High.ToString()
                },
                new NameValuePair
                {
                    Name = SyncQuality.Medium.ToString(),
                    Value = SyncQuality.Medium.ToString()
                },
                new NameValuePair
                {
                    Name = SyncQuality.Low.ToString(),
                    Value = SyncQuality.Low.ToString()
                }
            };
        }
    }
}
