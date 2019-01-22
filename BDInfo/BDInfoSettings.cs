
namespace BDInfo
{
    class BDInfoSettings
    {
        public static bool GenerateStreamDiagnostics => true;

        public static bool EnableSSIF => true;

        public static bool AutosaveReport => false;

        public static bool GenerateFrameDataFile => false;

        public static bool FilterLoopingPlaylists => true;

        public static bool FilterShortPlaylists => false;

        public static int FilterShortPlaylistsValue => 0;

        public static bool UseImagePrefix => false;

        public static string UseImagePrefixValue => null;

        /// <summary>
        /// Setting this to false throws an IComparer error on some discs.
        /// </summary>
        public static bool KeepStreamOrder => true;

        public static bool GenerateTextSummary => false;

        public static string LastPath => string.Empty;
    }
}
