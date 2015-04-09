
namespace MediaBrowser.Model.Sync
{
    public class SyncOptions
    {
        public string TemporaryPath { get; set; }
        public long UploadSpeedLimitBytes { get; set; }
        public int TranscodingCpuCoreLimit { get; set; }
        public bool EnableFullSpeedTranscoding { get; set; }

        public SyncOptions()
        {
            TranscodingCpuCoreLimit = 1;
        }
    }
}
