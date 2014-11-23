
namespace MediaBrowser.Server.Mono.Native
{
    /// <summary>
    /// Class NativeApp
    /// </summary>
    public class NativeApp : BaseMonoApp
    {
        /// <summary>
        /// Shutdowns this instance.
        /// </summary>
        public override void Shutdown()
        {
            MainClass.Shutdown();
        }
    }
}
