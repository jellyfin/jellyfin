using System.Diagnostics;

namespace MediaBrowser.Controller.Diagnostics
{
    /// <summary>
    /// Interface IProcessManager
    /// </summary>
    public interface IProcessManager
    {
        /// <summary>
        /// Gets a value indicating whether [supports suspension].
        /// </summary>
        /// <value><c>true</c> if [supports suspension]; otherwise, <c>false</c>.</value>
        bool SupportsSuspension { get; }

        /// <summary>
        /// Suspends the process.
        /// </summary>
        /// <param name="process">The process.</param>
        void SuspendProcess(Process process);

        /// <summary>
        /// Resumes the process.
        /// </summary>
        /// <param name="process">The process.</param>
        void ResumeProcess(Process process);
    }
}
