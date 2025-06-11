using System;
using System.Threading;
using System.Threading.Tasks;
using MediaBrowser.Model.Configuration;

namespace MediaBrowser.Controller.LibraryTaskScheduler;

/// <summary>
/// Provides a shared scheduler to run library related tasks based on the <see cref="ServerConfiguration.LibraryScanFanoutConcurrency"/>.
/// </summary>
public interface ILimitedConcurrencyLibraryScheduler
{
    /// <summary>
    /// Enqueues an action that will be invoked with the set data.
    /// </summary>
    /// <typeparam name="T">The data Type.</typeparam>
    /// <param name="data">The data.</param>
    /// <param name="worker">The callback to process the data.</param>
    /// <param name="progress">A progress reporter.</param>
    /// <param name="cancellationToken">Stop token.</param>
    /// <returns>A task that finishes when all data has been processed by the worker.</returns>
    Task Enqueue<T>(T[] data, Func<T, IProgress<double>, Task> worker, IProgress<double> progress, CancellationToken cancellationToken);
}
