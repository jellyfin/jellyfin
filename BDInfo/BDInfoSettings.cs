
namespace BDInfo
{
    class BDInfoSettings
    {
        public static bool GenerateStreamDiagnostics
        {
            get
            {
                return true;
            }
        }

        public static bool EnableSSIF
        {
            get
            {
                return true;
            }
        }

        public static bool AutosaveReport
        {
            get
            {
                return false;
            }
        }

        public static bool GenerateFrameDataFile
        {
            get
            {
                return false;
            }
        }

        public static bool FilterLoopingPlaylists
        {
            get
            {
                return true;
            }
        }

        public static bool FilterShortPlaylists
        {
            get
            {
                return false;
            }
        }

        public static int FilterShortPlaylistsValue
        {
            get
            {
                return 0;
            }
        }

        public static bool UseImagePrefix
        {
            get
            {
                return false;
            }
        }

        public static string UseImagePrefixValue
        {
            get
            {
                return null;
            }
        }

        /// <summary>
        /// Setting this to false throws an IComparer error on some discs.
        /// </summary>
        public static bool KeepStreamOrder
        {
            get
            {
                return true;
            }
        }

        public static bool GenerateTextSummary
        {
            get
            {
                return false;
            }
        }

        public static string LastPath
        {
            get
            {
                return string.Empty;
            }
        }
    }
}
