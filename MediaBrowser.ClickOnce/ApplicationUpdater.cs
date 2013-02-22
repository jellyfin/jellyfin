using System;
using System.ComponentModel;
using System.Deployment.Application;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.ClickOnce
{
    /// <summary>
    /// Class ApplicationUpdater
    /// </summary>
    public class ApplicationUpdater
    {
        /// <summary>
        /// The _task completion source
        /// </summary>
        private TaskCompletionSource<AsyncCompletedEventArgs> _taskCompletionSource;

        /// <summary>
        /// The _progress
        /// </summary>
        private IProgress<double> _progress;

        /// <summary>
        /// Updates the application
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{AsyncCompletedEventArgs}.</returns>
        /// <exception cref="System.InvalidOperationException">Current deployment is not network deployed.</exception>
        public Task<AsyncCompletedEventArgs> UpdateApplication(CancellationToken cancellationToken, IProgress<double> progress)
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                throw new InvalidOperationException("Current deployment is not network deployed.");
            }

            _progress = progress;

            _taskCompletionSource = new TaskCompletionSource<AsyncCompletedEventArgs>();

            var deployment = ApplicationDeployment.CurrentDeployment;

            cancellationToken.Register(deployment.UpdateAsyncCancel);

            cancellationToken.ThrowIfCancellationRequested();

            deployment.UpdateCompleted += deployment_UpdateCompleted;
            deployment.UpdateProgressChanged += deployment_UpdateProgressChanged;

            deployment.UpdateAsync();

            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// Handles the UpdateCompleted event of the deployment control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="AsyncCompletedEventArgs" /> instance containing the event data.</param>
        void deployment_UpdateCompleted(object sender, AsyncCompletedEventArgs e)
        {
            var deployment = ApplicationDeployment.CurrentDeployment;

            deployment.UpdateCompleted -= deployment_UpdateCompleted;
            deployment.UpdateProgressChanged -= deployment_UpdateProgressChanged;

            if (e.Error != null)
            {
                _taskCompletionSource.SetException(e.Error);
            }
            else if (e.Cancelled)
            {
                _taskCompletionSource.SetCanceled();
            }
            else
            {
                _taskCompletionSource.SetResult(e);
            }
        }

        /// <summary>
        /// Handles the UpdateProgressChanged event of the deployment control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DeploymentProgressChangedEventArgs" /> instance containing the event data.</param>
        void deployment_UpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            _progress.Report(e.ProgressPercentage);
        }
    }
}
