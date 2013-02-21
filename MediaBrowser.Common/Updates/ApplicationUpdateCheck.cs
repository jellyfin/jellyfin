using MediaBrowser.Model.Tasks;
using System;
using System.Deployment.Application;
using System.Threading;
using System.Threading.Tasks;

namespace MediaBrowser.Common.Updates
{
    /// <summary>
    /// Class ApplicationUpdateCheck
    /// </summary>
    public class ApplicationUpdateCheck
    {
        /// <summary>
        /// The _task completion source
        /// </summary>
        private TaskCompletionSource<CheckForUpdateCompletedEventArgs> _taskCompletionSource;

        /// <summary>
        /// The _progress
        /// </summary>
        private IProgress<TaskProgress> _progress;

        /// <summary>
        /// Checks for application update.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="progress">The progress.</param>
        /// <returns>Task{CheckForUpdateCompletedEventArgs}.</returns>
        /// <exception cref="System.InvalidOperationException">Current deployment is not a ClickOnce deployment</exception>
        public Task<CheckForUpdateCompletedEventArgs> CheckForApplicationUpdate(CancellationToken cancellationToken, IProgress<TaskProgress> progress)
        {
            if (!ApplicationDeployment.IsNetworkDeployed)
            {
                throw new InvalidOperationException("Current deployment is not network deployed.");
            }

            _progress = progress;
            
            _taskCompletionSource = new TaskCompletionSource<CheckForUpdateCompletedEventArgs>();

            var deployment = ApplicationDeployment.CurrentDeployment;

            cancellationToken.Register(deployment.CheckForUpdateAsyncCancel);

            cancellationToken.ThrowIfCancellationRequested();

            deployment.CheckForUpdateCompleted += deployment_CheckForUpdateCompleted;
            deployment.CheckForUpdateProgressChanged += deployment_CheckForUpdateProgressChanged;

            deployment.CheckForUpdateAsync();

            return _taskCompletionSource.Task;
        }

        /// <summary>
        /// Handles the CheckForUpdateCompleted event of the deployment control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="CheckForUpdateCompletedEventArgs" /> instance containing the event data.</param>
        void deployment_CheckForUpdateCompleted(object sender, CheckForUpdateCompletedEventArgs e)
        {
            var deployment = ApplicationDeployment.CurrentDeployment;

            deployment.CheckForUpdateCompleted -= deployment_CheckForUpdateCompleted;
            deployment.CheckForUpdateProgressChanged -= deployment_CheckForUpdateProgressChanged;

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
        /// Handles the CheckForUpdateProgressChanged event of the deployment control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="DeploymentProgressChangedEventArgs" /> instance containing the event data.</param>
        void deployment_CheckForUpdateProgressChanged(object sender, DeploymentProgressChangedEventArgs e)
        {
            _progress.Report(new TaskProgress { PercentComplete = e.ProgressPercentage });
        }
    }
}
