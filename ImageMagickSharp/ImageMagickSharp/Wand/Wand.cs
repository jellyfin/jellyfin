using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ImageMagickSharp
{
    /// <summary> A wand. </summary>
    public class Wand
    {

        #region [Singleton]

        /// <summary> The instance. </summary>
        private static readonly Lazy<Wand> _Instance = new Lazy<Wand>(() => new Wand());

        /// <summary> Gets the instance. </summary>
        /// <value> The instance. </value>
        protected static Wand Instance
        {
            get
            {
                return _Instance.Value;
            }
        }

        #endregion

        #region [Wand Initializer]

        /// <summary> The is initialized. </summary>
        private static bool _IsInitialized = false;

        /// <summary> Initializes a new instance of the Wand class. </summary>
        private Wand()
        {
            this.InitializeEnvironment();
        }

        /// <summary> Sets magick coder module path. </summary>
        /// <param name="path"> Full pathname of the file. </param>
        public static void SetMagickCoderModulePath(string path)
        {
            Environment.SetEnvironmentVariable("MAGICK_CODER_MODULE_PATH", path);
        }

        /// <summary>
        /// Sets the magick coder module path.
        /// </summary>
        /// <param name="threadCount">The thread count.</param>
        public static void SetMagickThreadCount(int threadCount)
        {
            Environment.SetEnvironmentVariable("MAGICK_THREAD_LIMIT", threadCount.ToString(CultureInfo.InvariantCulture));
        }
        
        /// <summary> Sets magick configure path. </summary>
		/// <param name="path"> Full pathname of the file. </param>
		internal static void SetMagickConfigurePath(string path)
		{
			Environment.SetEnvironmentVariable("MAGICK_CONFIGURE_PATH", path);
		}

		/// <summary> Sets magick font path. </summary>
		/// <param name="path"> Full pathname of the file. </param>
		internal static void SetMagickFontPath(string path)
		{
			Environment.SetEnvironmentVariable("MAGICK_FONT_PATH", path);
		}

        /// <summary> Initializes the environment. </summary>
        protected void InitializeEnvironment()
        {
            if (!_IsInitialized)
            {
                WandInterop.MagickWandGenesis();
                _IsInitialized = WandInterop.IsMagickWandInstantiated();
                if (!_IsInitialized)
                    throw new Exception("Cannot Instantiate Wand");
            }
        }

        /// <summary> Dispose environment. </summary>
        protected void DisposeEnvironment()
        {
            if (IsInitialized)
            {
                WandInterop.MagickWandTerminus();
                _IsInitialized = false;
            }
        }

        /// <summary> Finalizes an instance of the ImageMagickSharp.Wand class. </summary>
        ~Wand()
        {
            this.DisposeEnvironment();
        }

        #endregion

        #region [Wand Properties]

        /// <summary> Gets a value indicating whether this object is initialized. </summary>
        /// <value> true if this object is initialized, false if not. </value>
        internal static bool IsInitialized
        {
            get
            {
                return _IsInitialized;
            }
        }

        /// <summary> Gets a value indicating whether this object is wand instantiated. </summary>
        /// <value> true if this object is wand instantiated, false if not. </value>
        internal static bool IsWandInstantiated
        {
            get
            {
                return WandInterop.IsMagickWandInstantiated();
            }
        }

        /// <summary> Gets the version string. </summary>
        /// <value> The version string. </value>
        public static string VersionString
        {
            get
            {
                EnsureInitialized();
                IntPtr version;
                return WandNativeString.Load(WandInterop.MagickGetVersion(out version), false);
            }
        }

        /// <summary> Gets the version number. </summary>
        /// <value> The version number. </value>
        private static int VersionNumber
        {
            get
            {
                EnsureInitialized();
                IntPtr version;
                WandInterop.MagickGetVersion(out version);
                return (int)version;
            }
        }

        /// <summary> Gets the version number string. </summary>
        /// <value> The version number string. </value>
        private static string VersionNumberString
        {
            get
            {
                return string.Join(".", VersionNumber.ToString("x").ToArray());
            }
        }

        #endregion

        #region [Wand Methods]

        /// <summary> Opens the environment. </summary>
        internal static void OpenEnvironment()
        {
            Wand.Instance.InitializeEnvironment();
        }

        /// <summary> Closes the environment. </summary>
        public static void CloseEnvironment()
        {
            if (_Instance != null && _Instance.Value != null)
            {
                _Instance.Value.DisposeEnvironment();
            }

        }

        /// <summary> Ensures that initialized. </summary>
        internal static void EnsureInitialized()
        {
            if (!_IsInitialized)
                Wand.Instance.InitializeEnvironment();
        }

        /// <summary> Gets the handle. </summary>
        /// <returns> The handle. </returns>
        internal static IntPtr GetHandle()
        {
            IntPtr version;
            return WandInterop.MagickGetVersion(out version);
        }

        /// <summary> Query if 'wand' is magick wand. </summary>
        /// <param name="wand"> The wand. </param>
        /// <returns> true if magick wand, false if not. </returns>
        /*private static bool IsMagickWand(IntPtr wand)
        {
            return WandInterop.IsMagickWand(wand);
        }*/

        /// <summary> Command genesis. </summary>
        /// <param name="image_info"> Information describing the image. </param>
        /// <param name="command"> The command. </param>
        /// <param name="argc"> The argc. </param>
        /// <param name="argv"> The argv. </param>
        /// <param name="metadata"> The metadata. </param>
        /// <param name="exception"> The exception. </param>
        /// <returns> true if it succeeds, false if it fails. </returns>
		/*private static bool CommandGenesis(IntPtr image_info, MagickCommandType command, int argc, string[] argv, byte[] metadata, IntPtr exception)
        {
           return WandInterop.MagickCommandGenesis(image_info, command, argc, argv, metadata, ref exception);
			//return WandInterop.MagickCommandGenesis(image_info, command, argc, argv);
        }*/

		/// <summary> Queries the formats. </summary>
		/// <param name="pattern"> Specifies the pattern. </param>
		/// <returns> An array of string. </returns>
		internal static List<string> QueryFormats(string pattern)
		{
			EnsureInitialized();
			IntPtr number_formats = IntPtr.Zero;
            IntPtr format = WandInterop.MagickQueryFormats("*", ref number_formats);
			IntPtr[] rowArray = new IntPtr[(int)number_formats];
			Marshal.Copy(format, rowArray, 0, (int)number_formats);
			List<string> val = rowArray.Select(x => WandNativeString.Load(x)).ToList();
			if (pattern == "*")
				return val;
			return val.FindAll(x => x.Equals(pattern, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary> Queries format from file. </summary>
		/// <param name="file"> The file. </param>
		/// <returns> true if it succeeds, false if it fails. </returns>
		/*private static bool QueryFormatFromFile(string file)
		{
			return QueryFormats(Path.GetExtension(file).Replace(".", "")).Count > 0;
		}*/

		/// <summary> Queries the fonts. </summary>
		/// <param name="pattern"> Specifies the pattern. </param>
		/// <returns> An array of string. </returns>
		/*private static List<string> QueryFonts(string pattern)
		{
			EnsureInitialized();
			using (var stringFormat = new WandNativeString("*"))
			{
				int number_formats = 0;
				IntPtr format = WandInterop.MagickQueryFonts(stringFormat.Pointer, ref number_formats);
				IntPtr[] rowArray = new IntPtr[number_formats];
				Marshal.Copy(format, rowArray, 0, number_formats);
				List<string> val = rowArray.Select(x => WandNativeString.Load(x)).ToList();
				if (pattern == "*")
					return val;
				return val.FindAll(x=> x.Equals(pattern, StringComparison.InvariantCultureIgnoreCase));
			}
		}*/

        #endregion

    }
}
