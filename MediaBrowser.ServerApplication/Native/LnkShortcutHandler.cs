using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using CommonIO;

namespace MediaBrowser.ServerApplication.Native
{
    public class LnkShortcutHandler : IShortcutHandler
    {
        public string Extension
        {
            get { return ".lnk"; }
        }

        public string Resolve(string shortcutPath)
        {
            var link = new ShellLink();
            ((IPersistFile)link).Load(shortcutPath, NativeMethods.STGM_READ);
            // ((IShellLinkW)link).Resolve(hwnd, 0) 
            var sb = new StringBuilder(NativeMethods.MAX_PATH);
            WIN32_FIND_DATA data;
            ((IShellLinkW)link).GetPath(sb, sb.Capacity, out data, 0);
            return sb.ToString();
        }

        public void Create(string shortcutPath, string targetPath)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Class NativeMethods
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public static class NativeMethods
    {
        /// <summary>
        /// The MA x_ PATH
        /// </summary>
        public const int MAX_PATH = 260;
        /// <summary>
        /// The MA x_ ALTERNATE
        /// </summary>
        public const int MAX_ALTERNATE = 14;
        /// <summary>
        /// The INVALI d_ HANDL e_ VALUE
        /// </summary>
        public static IntPtr INVALID_HANDLE_VALUE = new IntPtr(-1);
        /// <summary>
        /// The STG m_ READ
        /// </summary>
        public const uint STGM_READ = 0;
    }

    /// <summary>
    /// Struct FILETIME
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct FILETIME
    {
        /// <summary>
        /// The dw low date time
        /// </summary>
        public uint dwLowDateTime;
        /// <summary>
        /// The dw high date time
        /// </summary>
        public uint dwHighDateTime;
    }

    /// <summary>
    /// Struct WIN32_FIND_DATA
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct WIN32_FIND_DATA
    {
        /// <summary>
        /// The dw file attributes
        /// </summary>
        public FileAttributes dwFileAttributes;
        /// <summary>
        /// The ft creation time
        /// </summary>
        public FILETIME ftCreationTime;
        /// <summary>
        /// The ft last access time
        /// </summary>
        public FILETIME ftLastAccessTime;
        /// <summary>
        /// The ft last write time
        /// </summary>
        public FILETIME ftLastWriteTime;
        /// <summary>
        /// The n file size high
        /// </summary>
        public int nFileSizeHigh;
        /// <summary>
        /// The n file size low
        /// </summary>
        public int nFileSizeLow;
        /// <summary>
        /// The dw reserved0
        /// </summary>
        public int dwReserved0;
        /// <summary>
        /// The dw reserved1
        /// </summary>
        public int dwReserved1;

        /// <summary>
        /// The c file name
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MAX_PATH)]
        public string cFileName;

        /// <summary>
        /// This will always be null when FINDEX_INFO_LEVELS = basic
        /// </summary>
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = NativeMethods.MAX_ALTERNATE)]
        public string cAlternate;

        /// <summary>
        /// Gets or sets the path.
        /// </summary>
        /// <value>The path.</value>
        public string Path { get; set; }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return Path ?? string.Empty;
        }
    }

    /// <summary>
    /// Enum SLGP_FLAGS
    /// </summary>
    [Flags]
    public enum SLGP_FLAGS
    {
        /// <summary>
        /// Retrieves the standard short (8.3 format) file name
        /// </summary>
        SLGP_SHORTPATH = 0x1,
        /// <summary>
        /// Retrieves the Universal Naming Convention (UNC) path name of the file
        /// </summary>
        SLGP_UNCPRIORITY = 0x2,
        /// <summary>
        /// Retrieves the raw path name. A raw path is something that might not exist and may include environment variables that need to be expanded
        /// </summary>
        SLGP_RAWPATH = 0x4
    }
    /// <summary>
    /// Enum SLR_FLAGS
    /// </summary>
    [Flags]
    public enum SLR_FLAGS
    {
        /// <summary>
        /// Do not display a dialog box if the link cannot be resolved. When SLR_NO_UI is set,
        /// the high-order word of fFlags can be set to a time-out value that specifies the
        /// maximum amount of time to be spent resolving the link. The function returns if the
        /// link cannot be resolved within the time-out duration. If the high-order word is set
        /// to zero, the time-out duration will be set to the default value of 3,000 milliseconds
        /// (3 seconds). To specify a value, set the high word of fFlags to the desired time-out
        /// duration, in milliseconds.
        /// </summary>
        SLR_NO_UI = 0x1,
        /// <summary>
        /// Obsolete and no longer used
        /// </summary>
        SLR_ANY_MATCH = 0x2,
        /// <summary>
        /// If the link object has changed, update its path and list of identifiers.
        /// If SLR_UPDATE is set, you do not need to call IPersistFile::IsDirty to determine
        /// whether or not the link object has changed.
        /// </summary>
        SLR_UPDATE = 0x4,
        /// <summary>
        /// Do not update the link information
        /// </summary>
        SLR_NOUPDATE = 0x8,
        /// <summary>
        /// Do not execute the search heuristics
        /// </summary>
        SLR_NOSEARCH = 0x10,
        /// <summary>
        /// Do not use distributed link tracking
        /// </summary>
        SLR_NOTRACK = 0x20,
        /// <summary>
        /// Disable distributed link tracking. By default, distributed link tracking tracks
        /// removable media across multiple devices based on the volume name. It also uses the
        /// Universal Naming Convention (UNC) path to track remote file systems whose drive letter
        /// has changed. Setting SLR_NOLINKINFO disables both types of tracking.
        /// </summary>
        SLR_NOLINKINFO = 0x40,
        /// <summary>
        /// Call the Microsoft Windows Installer
        /// </summary>
        SLR_INVOKE_MSI = 0x80
    }

    /// <summary>
    /// The IShellLink interface allows Shell links to be created, modified, and resolved
    /// </summary>
    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("000214F9-0000-0000-C000-000000000046")]
    public interface IShellLinkW
    {
        /// <summary>
        /// Retrieves the path and file name of a Shell link object
        /// </summary>
        /// <param name="pszFile">The PSZ file.</param>
        /// <param name="cchMaxPath">The CCH max path.</param>
        /// <param name="pfd">The PFD.</param>
        /// <param name="fFlags">The f flags.</param>
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATA pfd, SLGP_FLAGS fFlags);
        /// <summary>
        /// Retrieves the list of item identifiers for a Shell link object
        /// </summary>
        /// <param name="ppidl">The ppidl.</param>
        void GetIDList(out IntPtr ppidl);
        /// <summary>
        /// Sets the pointer to an item identifier list (PIDL) for a Shell link object.
        /// </summary>
        /// <param name="pidl">The pidl.</param>
        void SetIDList(IntPtr pidl);
        /// <summary>
        /// Retrieves the description string for a Shell link object
        /// </summary>
        /// <param name="pszName">Name of the PSZ.</param>
        /// <param name="cchMaxName">Name of the CCH max.</param>
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        /// <summary>
        /// Sets the description for a Shell link object. The description can be any application-defined string
        /// </summary>
        /// <param name="pszName">Name of the PSZ.</param>
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        /// <summary>
        /// Retrieves the name of the working directory for a Shell link object
        /// </summary>
        /// <param name="pszDir">The PSZ dir.</param>
        /// <param name="cchMaxPath">The CCH max path.</param>
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        /// <summary>
        /// Sets the name of the working directory for a Shell link object
        /// </summary>
        /// <param name="pszDir">The PSZ dir.</param>
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        /// <summary>
        /// Retrieves the command-line arguments associated with a Shell link object
        /// </summary>
        /// <param name="pszArgs">The PSZ args.</param>
        /// <param name="cchMaxPath">The CCH max path.</param>
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        /// <summary>
        /// Sets the command-line arguments for a Shell link object
        /// </summary>
        /// <param name="pszArgs">The PSZ args.</param>
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        /// <summary>
        /// Retrieves the hot key for a Shell link object
        /// </summary>
        /// <param name="pwHotkey">The pw hotkey.</param>
        void GetHotkey(out short pwHotkey);
        /// <summary>
        /// Sets a hot key for a Shell link object
        /// </summary>
        /// <param name="wHotkey">The w hotkey.</param>
        void SetHotkey(short wHotkey);
        /// <summary>
        /// Retrieves the show command for a Shell link object
        /// </summary>
        /// <param name="piShowCmd">The pi show CMD.</param>
        void GetShowCmd(out int piShowCmd);
        /// <summary>
        /// Sets the show command for a Shell link object. The show command sets the initial show state of the window.
        /// </summary>
        /// <param name="iShowCmd">The i show CMD.</param>
        void SetShowCmd(int iShowCmd);
        /// <summary>
        /// Retrieves the location (path and index) of the icon for a Shell link object
        /// </summary>
        /// <param name="pszIconPath">The PSZ icon path.</param>
        /// <param name="cchIconPath">The CCH icon path.</param>
        /// <param name="piIcon">The pi icon.</param>
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
            int cchIconPath, out int piIcon);
        /// <summary>
        /// Sets the location (path and index) of the icon for a Shell link object
        /// </summary>
        /// <param name="pszIconPath">The PSZ icon path.</param>
        /// <param name="iIcon">The i icon.</param>
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        /// <summary>
        /// Sets the relative path to the Shell link object
        /// </summary>
        /// <param name="pszPathRel">The PSZ path rel.</param>
        /// <param name="dwReserved">The dw reserved.</param>
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        /// <summary>
        /// Attempts to find the target of a Shell link, even if it has been moved or renamed
        /// </summary>
        /// <param name="hwnd">The HWND.</param>
        /// <param name="fFlags">The f flags.</param>
        void Resolve(IntPtr hwnd, SLR_FLAGS fFlags);
        /// <summary>
        /// Sets the path and file name of a Shell link object
        /// </summary>
        /// <param name="pszFile">The PSZ file.</param>
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);

    }

    /// <summary>
    /// Interface IPersist
    /// </summary>
    [ComImport, Guid("0000010c-0000-0000-c000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersist
    {
        /// <summary>
        /// Gets the class ID.
        /// </summary>
        /// <param name="pClassID">The p class ID.</param>
        [PreserveSig]
        void GetClassID(out Guid pClassID);
    }

    /// <summary>
    /// Interface IPersistFile
    /// </summary>
    [ComImport, Guid("0000010b-0000-0000-C000-000000000046"),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface IPersistFile : IPersist
    {
        /// <summary>
        /// Gets the class ID.
        /// </summary>
        /// <param name="pClassID">The p class ID.</param>
        new void GetClassID(out Guid pClassID);
        /// <summary>
        /// Determines whether this instance is dirty.
        /// </summary>
        [PreserveSig]
        int IsDirty();

        /// <summary>
        /// Loads the specified PSZ file name.
        /// </summary>
        /// <param name="pszFileName">Name of the PSZ file.</param>
        /// <param name="dwMode">The dw mode.</param>
        [PreserveSig]
        void Load([In, MarshalAs(UnmanagedType.LPWStr)]
            string pszFileName, uint dwMode);

        /// <summary>
        /// Saves the specified PSZ file name.
        /// </summary>
        /// <param name="pszFileName">Name of the PSZ file.</param>
        /// <param name="remember">if set to <c>true</c> [remember].</param>
        [PreserveSig]
        void Save([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
            [In, MarshalAs(UnmanagedType.Bool)] bool remember);

        /// <summary>
        /// Saves the completed.
        /// </summary>
        /// <param name="pszFileName">Name of the PSZ file.</param>
        [PreserveSig]
        void SaveCompleted([In, MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

        /// <summary>
        /// Gets the cur file.
        /// </summary>
        /// <param name="ppszFileName">Name of the PPSZ file.</param>
        [PreserveSig]
        void GetCurFile([In, MarshalAs(UnmanagedType.LPWStr)] string ppszFileName);
    }

    // CLSID_ShellLink from ShlGuid.h 
    /// <summary>
    /// Class ShellLink
    /// </summary>
    [
        ComImport,
        Guid("00021401-0000-0000-C000-000000000046")
    ]
    public class ShellLink
    {
    }
}
