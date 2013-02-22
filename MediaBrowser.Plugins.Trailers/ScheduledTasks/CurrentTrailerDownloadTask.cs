using MediaBrowser.Common.IO;
using MediaBrowser.Common.ScheduledTasks;
using MediaBrowser.Common.Serialization;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Model.Net;
using MediaBrowser.Model.Tasks;
using MediaBrowser.Plugins.Trailers.Entities;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Plugins.Trailers.ScheduledTasks
{
    /// <summary>
    /// Downloads trailers from the web at scheduled times
    /// </summary>
    [Export(typeof(IScheduledTask))]
    public class CurrentTrailerDownloadTask : BaseScheduledTask<Kernel>
    {
        /// <summary>
        /// Creates the triggers that define when the task will run
        /// </summary>
        /// <returns>IEnumerable{BaseTaskTrigger}.</returns>
        protected override IEnumerable<BaseTaskTrigger> GetDefaultTriggers()
        {
            var trigger = new DailyTrigger { TimeOfDay = TimeSpan.FromHours(2) }; //2am

            return new[] { trigger };
        }

        /// <summary>
        /// Returns the task to be executed
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task.</returns>
        protected override async Task ExecuteInternal(CancellationToken cancellationToken, IProgress<double> progress)
        {
            // Get the list of trailers
            var trailers = await AppleTrailerListingDownloader.GetTrailerList(cancellationToken).ConfigureAwait(false);

            progress.Report(1);

            var trailersToDownload = trailers.Where(t => !IsOldTrailer(t.Video)).ToList();

            cancellationToken.ThrowIfCancellationRequested();

            var numComplete = 0;

            // Fetch them all in parallel
            var tasks = trailersToDownload.Select(t => Task.Run(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    await DownloadTrailer(t, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Logger.ErrorException("Error downloading {0}", ex, t.TrailerUrl);
                }

                // Update progress
                lock (progress)
                {
                    numComplete++;
                    double percent = numComplete;
                    percent /= trailersToDownload.Count;

                    // Leave 1% for DeleteOldTrailers
                    progress.Report((99 * percent) + 1);
                }
            }));

            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.WhenAll(tasks).ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();
            
            if (Plugin.Instance.Configuration.DeleteOldTrailers)
            {
                // Enforce MaxTrailerAge
                DeleteOldTrailers();
            }

            progress.Report(100);
        }

        /// <summary>
        /// Downloads a single trailer into the trailers directory
        /// </summary>
        /// <param name="trailer">The trailer.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task.</returns>
        private async Task DownloadTrailer(TrailerInfo trailer, CancellationToken cancellationToken)
        {
            // Construct the trailer foldername
            var folderName = FileSystem.GetValidFilename(trailer.Video.Name);

            if (trailer.Video.ProductionYear.HasValue)
            {
                folderName += string.Format(" ({0})", trailer.Video.ProductionYear);
            }

            var folderPath = Path.Combine(Plugin.Instance.DownloadPath, folderName);

            // Figure out which image we're going to download
            var imageUrl = trailer.HdImageUrl ?? trailer.ImageUrl;

            // Construct the video filename (to match the folder name)
            var videoFileName = Path.ChangeExtension(folderName, Path.GetExtension(trailer.TrailerUrl));

            // Construct the image filename (folder + original extension)
            var imageFileName = Path.ChangeExtension("folder", Path.GetExtension(imageUrl));

            // Construct full paths
            var videoFilePath = Path.Combine(folderPath, videoFileName);
            var imageFilePath = Path.Combine(folderPath, imageFileName);

            // Create tasks to download each of them, if we don't already have them
            Task<string> videoTask = null;
            Task<MemoryStream> imageTask = null;

            var tasks = new List<Task>();

            if (!File.Exists(videoFilePath))
            {
                Logger.Info("Downloading trailer: " + trailer.TrailerUrl);

                // Fetch the video to a temp file because it's too big to put into a MemoryStream
                videoTask = Kernel.HttpManager.FetchToTempFile(trailer.TrailerUrl, Kernel.ResourcePools.AppleTrailerVideos, cancellationToken, new Progress<double> { }, "QuickTime/7.6.2");
                tasks.Add(videoTask);
            }

            if (!string.IsNullOrWhiteSpace(imageUrl) && !File.Exists(imageFilePath))
            {
                // Fetch the image to a memory stream
                Logger.Info("Downloading trailer image: " + imageUrl);
                imageTask = Kernel.HttpManager.FetchToMemoryStream(imageUrl, Kernel.ResourcePools.AppleTrailerImages, cancellationToken);
                tasks.Add(imageTask);
            }

            try
            {
                // Wait for both downloads to finish
                await Task.WhenAll(tasks).ConfigureAwait(false);
            }
            catch (HttpException ex)
            {
                Logger.ErrorException("Error downloading trailer file or image", ex);
            }

            var videoFailed = false;
            var directoryEnsured = false;

            // Proces the video file task result
            if (videoTask != null)
            {
                if (videoTask.Status == TaskStatus.RanToCompletion)
                {
                    EnsureDirectory(folderPath);

                    directoryEnsured = true;

                    // Move the temp file to the final destination
                    try
                    {
                        File.Move(videoTask.Result, videoFilePath);
                    }
                    catch (IOException ex)
                    {
                        Logger.ErrorException("Error moving temp file", ex);
                        File.Delete(videoTask.Result);
                        videoFailed = true;
                    }
                }
                else
                {
                    Logger.Info("Trailer download failed: " + trailer.TrailerUrl);

                    // Don't bother with the image if the video download failed
                    videoFailed = true;
                }
            }

            // Process the image file task result
            if (imageTask != null && !videoFailed && imageTask.Status == TaskStatus.RanToCompletion)
            {
                if (!directoryEnsured)
                {
                    EnsureDirectory(folderPath);
                }

                try
                {
                    // Save the image to the file system
                    using (var fs = new FileStream(imageFilePath, FileMode.Create, FileAccess.Write, FileShare.Read, StreamDefaults.DefaultFileStreamBufferSize, FileOptions.Asynchronous))
                    {
                        using (var sourceStream = imageTask.Result)
                        {
                            await sourceStream.CopyToAsync(fs).ConfigureAwait(false);
                        }
                    }
                }
                catch (IOException ex)
                {
                    Logger.ErrorException("Error saving image to file system", ex);
                }
            }

            // Save metadata only if the video was downloaded
            if (!videoFailed && videoTask != null)
            {
                JsonSerializer.SerializeToFile(trailer.Video, Path.Combine(folderPath, "trailer.json"));
            }
        }

        /// <summary>
        /// Determines whether [is old trailer] [the specified trailer].
        /// </summary>
        /// <param name="trailer">The trailer.</param>
        /// <returns><c>true</c> if [is old trailer] [the specified trailer]; otherwise, <c>false</c>.</returns>
        private bool IsOldTrailer(Trailer trailer)
        {
            if (!Plugin.Instance.Configuration.MaxTrailerAge.HasValue)
            {
                return false;
            }

            if (!trailer.PremiereDate.HasValue)
            {
                return false;
            }

            var now = DateTime.UtcNow;

            // Not old if it still hasn't premiered.
            if (now < trailer.PremiereDate.Value)
            {
                return false;
            }

            return (DateTime.UtcNow - trailer.PremiereDate.Value).TotalDays >
                   Plugin.Instance.Configuration.MaxTrailerAge.Value;
        }

        /// <summary>
        /// Deletes trailers that are older than the supplied date
        /// </summary>
        private void DeleteOldTrailers()
        {
            var collectionFolder = (Folder)Kernel.RootFolder.Children.First(c => c.GetType().Name.Equals(typeof(TrailerCollectionFolder).Name));

            foreach (var trailer in collectionFolder.RecursiveChildren.OfType<Trailer>().Where(IsOldTrailer))
            {
                Logger.Info("Deleting old trailer: " + trailer.Name);

                Directory.Delete(Path.GetDirectoryName(trailer.Path), true);
            }
        }

        /// <summary>
        /// Ensures the directory.
        /// </summary>
        /// <param name="path">The path.</param>
        private void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }

        /// <summary>
        /// Gets the name of the task
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get { return "Find current trailers"; }
        }

        /// <summary>
        /// Gets the category.
        /// </summary>
        /// <value>The category.</value>
        public override string Category
        {
            get
            {
                return "Trailers";
            }
        }

        /// <summary>
        /// Gets the description.
        /// </summary>
        /// <value>The description.</value>
        public override string Description
        {
            get { return "Searches the web for upcoming movie trailers, and downloads them based on your Trailer plugin settings."; }
        }
    }
}
