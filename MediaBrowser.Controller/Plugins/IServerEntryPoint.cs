using System;

namespace MediaBrowser.Controller.Plugins
{
    /// <summary>
    /// Interface IServerEntryPoint
    /// </summary>
    public interface IServerEntryPoint : IDisposable
    {
        /// <summary>
        /// Runs this instance.
        /// </summary>
        void Run();
    }

    public interface IRunBeforeStartup
    {

    }
}
