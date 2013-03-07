using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Controller.Configuration;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Controller.IO
{
    /// <summary>
    /// This class will manage our file system watching and modifications.  Any process that needs to
    /// modify the directories that the system is watching for changes should use the methods of
    /// this class to do so.  This way we can have the watchers correctly respond to only external changes.
    /// </summary>
    public class FileSystemManager : IDisposable
    {
        /// <summary>
        /// Gets or sets the directory watchers.
        /// </summary>
        /// <value>The directory watchers.</value>
        private DirectoryWatchers DirectoryWatchers { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSystemManager" /> class.
        /// </summary>
        /// <param name="logManager">The log manager.</param>
        /// <param name="taskManager">The task manager.</param>
        /// <param name="libraryManager">The library manager.</param>
        /// <param name="configurationManager">The configuration manager.</param>
        public FileSystemManager(ILogManager logManager, ITaskManager taskManager, ILibraryManager libraryManager, IServerConfigurationManager configurationManager)
        {
            DirectoryWatchers = new DirectoryWatchers(logManager, taskManager, libraryManager, configurationManager);
        }

        /// <summary>
        /// Start the directory watchers on our library folders
        /// </summary>
        public void StartWatchers()
        {
            DirectoryWatchers.Start();
        }

        /// <summary>
        /// Saves to library filesystem.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <param name="path">The path.</param>
        /// <param name="dataToSave">The data to save.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        /// <exception cref="System.ArgumentNullException"></exception>
        public async Task SaveToLibraryFilesystem(BaseItem item, string path, Stream dataToSave, CancellationToken cancellationToken)
        {
            if (item == null)
            {
                throw new ArgumentNullException();
            }
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException();
            }
            if (dataToSave == null)
            {
                throw new ArgumentNullException();
            }
            if (cancellationToken == null)
            {
                throw new ArgumentNullException();
            }
     
            cancellationToken.ThrowIfCancellationRequested();

            //Tell the watchers to ignore
            DirectoryWatchers.TemporarilyIgnore(path);

            //Make the mod

            dataToSave.Position = 0;

            try
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                {
                    await dataToSave.CopyToAsync(fs, StreamDefaults.DefaultCopyToBufferSize, cancellationToken).ConfigureAwait(false);

                    dataToSave.Dispose();

                    // If this is ever used for something other than metadata we can add a file type param
                    item.ResolveArgs.AddMetadataFile(path);
                }
            }
            finally
            {
                //Remove the ignore
                DirectoryWatchers.RemoveTempIgnore(path);
            }
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="dispose"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool dispose)
        {
            if (dispose)
            {
                DirectoryWatchers.Dispose();
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
    }
}
