using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Metrics
{
    /// <summary>
    /// Interface for metric backends.
    /// </summary>
    public interface IMetricsCollector
    {
        /// <summary>
        /// Initializes metrics.
        /// </summary>
        public void Initialize();
    }
}
