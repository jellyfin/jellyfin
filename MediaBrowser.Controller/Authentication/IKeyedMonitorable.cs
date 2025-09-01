using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.Authentication
{
    /// <summary>
    /// An interface that allows creation and monitoring of arbitrary data.
    /// </summary>
    /// <typeparam name="TData">Type of the data you want to monitor/update.</typeparam>
    public interface IKeyedMonitorable<TData>
    {
        /// <summary>
        /// Accepts new data to be monitored.
        /// </summary>
        /// <param name="data">The data you want to monitor.</param>
        /// <returns>A <see cref="MonitorData"/> object containing a key for monitoring and a key for updating the <typeparamref name="TData"/>.</returns>
        Task<MonitorData> Initiate(TData data);

        /// <summary>
        /// Get the data associated with the given <paramref name="monitorKey"/>.
        /// </summary>
        /// <param name="monitorKey">The monitor key.</param>
        /// <param name="waitForUpdate">A flag indicating whether or not to wait at most <paramref name="millisecondsTimeout"/> milliseconds for <see cref="Update(string, Action{TData})"/> to be called before returning. Note that after a timeout, a value is still returned, even if there has not been any update. Clients should check this on their side.</param>
        /// <param name="millisecondsTimeout">A timeout in milliseconds to wait when <paramref name="waitForUpdate"/> is true.</param>
        /// <returns>The data associated with the given <paramref name="monitorKey"/>, or null if not found.</returns>
        Task<TData?> GetData(string monitorKey, bool waitForUpdate = false, int millisecondsTimeout = 60000);

        /// <summary>
        /// Updates data associated with the given <paramref name="updateKey"/>.
        /// </summary>
        /// <param name="updateKey">The update key.</param>
        /// <param name="updater">The updater function used to update the data.</param>
        /// <returns>A boolean indicating whether or not the updating succeeded.</returns>
        Task<bool> Update(string updateKey, Action<TData> updater);
    }

    /// <summary>
    /// Data holder used by <see cref="IKeyedMonitorable{TData}"/>.
    /// </summary>
    /// <param name="MonitorKey">A key used to view/monitor the TData.</param>
    /// <param name="UpdateKey">A key used to update the TData.</param>
    public record struct MonitorData(string MonitorKey, string UpdateKey);
}
