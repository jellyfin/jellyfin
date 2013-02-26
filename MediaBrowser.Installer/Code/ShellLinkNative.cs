/**************************************************************************
*
* Filename:     ShellLinkNative.cs
* Author:       Mattias Sjögren (mattias@mvps.org)
*               http://www.msjogren.net/dotnet/
*
* Description:  Defines the native types used to manipulate shell shortcuts.
*
* Public types: enum SLR_FLAGS
*               enum SLGP_FLAGS
*               struct WIN32_FIND_DATA[A|W]
*               interface IPersistFile
*               interface IShellLink[A|W]
*               class ShellLink
*
*
* Copyright ©2001-2002, Mattias Sjögren
* 
**************************************************************************/

using System;
using System.Text;
using System.Runtime.InteropServices;

namespace MediaBrowser.Installer.Code
{
  // IShellLink.Resolve fFlags
  [Flags()]
  public enum SLR_FLAGS
  {
    SLR_NO_UI = 0x1,
    SLR_ANY_MATCH = 0x2,
    SLR_UPDATE = 0x4,
    SLR_NOUPDATE = 0x8,
    SLR_NOSEARCH = 0x10,
    SLR_NOTRACK = 0x20,
    SLR_NOLINKINFO = 0x40,
    SLR_INVOKE_MSI = 0x80
  }

  // IShellLink.GetPath fFlags
  [Flags()]
  public enum SLGP_FLAGS
  {
    SLGP_SHORTPATH = 0x1,
    SLGP_UNCPRIORITY = 0x2,
    SLGP_RAWPATH = 0x4
  }

  [StructLayoutAttribute(LayoutKind.Sequential, CharSet=CharSet.Ansi)]
  public struct WIN32_FIND_DATAA
  {
    public int dwFileAttributes;
    public FILETIME ftCreationTime;
    public FILETIME ftLastAccessTime;
    public FILETIME ftLastWriteTime;
    public int nFileSizeHigh;
    public int nFileSizeLow;
    public int dwReserved0;
    public int dwReserved1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
    public string cFileName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
    public string cAlternateFileName;
    private const int MAX_PATH = 260;
  }

  [StructLayoutAttribute(LayoutKind.Sequential, CharSet=CharSet.Unicode)]
  public struct WIN32_FIND_DATAW
  {
    public int dwFileAttributes;
    public FILETIME ftCreationTime;
    public FILETIME ftLastAccessTime;
    public FILETIME ftLastWriteTime;
    public int nFileSizeHigh;
    public int nFileSizeLow;
    public int dwReserved0;
    public int dwReserved1;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=MAX_PATH)]
    public string cFileName;
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst=14)]
    public string cAlternateFileName;
    private const int MAX_PATH = 260;
  }

  [
    ComImport(),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("0000010B-0000-0000-C000-000000000046")
  ]
  public interface IPersistFile
  {
    #region Methods inherited from IPersist

    void GetClassID(
      out Guid pClassID);

    #endregion

    [PreserveSig()]
    int IsDirty();

    void Load(
      [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
      int dwMode);

    void Save(
      [MarshalAs(UnmanagedType.LPWStr)] string pszFileName,
      [MarshalAs(UnmanagedType.Bool)] bool fRemember);

    void SaveCompleted(
      [MarshalAs(UnmanagedType.LPWStr)] string pszFileName);

    void GetCurFile(
      out IntPtr ppszFileName);

  }

  [
    ComImport(),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("000214EE-0000-0000-C000-000000000046")
  ]
  public interface IShellLinkA
  {
    void GetPath(
      [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszFile,
      int cchMaxPath,
      out WIN32_FIND_DATAA pfd,
      SLGP_FLAGS fFlags);

    void GetIDList(
      out IntPtr ppidl);

    void SetIDList(
      IntPtr pidl);

    void GetDescription(
      [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszName,
      int cchMaxName);

    void SetDescription(
      [MarshalAs(UnmanagedType.LPStr)] string pszName);

    void GetWorkingDirectory(
      [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszDir,
      int cchMaxPath);

    void SetWorkingDirectory(
      [MarshalAs(UnmanagedType.LPStr)] string pszDir);

    void GetArguments(
      [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszArgs,
      int cchMaxPath);

    void SetArguments(
      [MarshalAs(UnmanagedType.LPStr)] string pszArgs);

    void GetHotkey(
      out short pwHotkey);

    void SetHotkey(
      short wHotkey);

    void GetShowCmd(
      out int piShowCmd);

    void SetShowCmd(
      int iShowCmd);

    void GetIconLocation(
      [Out(), MarshalAs(UnmanagedType.LPStr)] StringBuilder pszIconPath,
      int cchIconPath,
      out int piIcon);

    void SetIconLocation(
      [MarshalAs(UnmanagedType.LPStr)] string pszIconPath,
      int iIcon);

    void SetRelativePath(
      [MarshalAs(UnmanagedType.LPStr)] string pszPathRel,
      int dwReserved);

    void Resolve(
      IntPtr hwnd,
      SLR_FLAGS fFlags);

    void SetPath(
      [MarshalAs(UnmanagedType.LPStr)] string pszFile);

  }

  [
    ComImport(),
    InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
    Guid("000214F9-0000-0000-C000-000000000046")
  ]
  public interface IShellLinkW
  {
    void GetPath(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile,
      int cchMaxPath,
      out WIN32_FIND_DATAW pfd,
      SLGP_FLAGS fFlags);

    void GetIDList(
      out IntPtr ppidl);

    void SetIDList(
      IntPtr pidl);

    void GetDescription(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName,
      int cchMaxName);

    void SetDescription(
      [MarshalAs(UnmanagedType.LPWStr)] string pszName);

    void GetWorkingDirectory(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir,
      int cchMaxPath);

    void SetWorkingDirectory(
      [MarshalAs(UnmanagedType.LPWStr)] string pszDir);

    void GetArguments(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs,
      int cchMaxPath);

    void SetArguments(
      [MarshalAs(UnmanagedType.LPWStr)] string pszArgs);

    void GetHotkey(
      out short pwHotkey);

    void SetHotkey(
      short wHotkey);

    void GetShowCmd(
      out int piShowCmd);

    void SetShowCmd(
      int iShowCmd);

    void GetIconLocation(
      [Out(), MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath,
      int cchIconPath,
      out int piIcon);

    void SetIconLocation(
      [MarshalAs(UnmanagedType.LPWStr)] string pszIconPath,
      int iIcon);

    void SetRelativePath(
      [MarshalAs(UnmanagedType.LPWStr)] string pszPathRel,
      int dwReserved);

    void Resolve(
      IntPtr hwnd,
      SLR_FLAGS fFlags);

    void SetPath(
      [MarshalAs(UnmanagedType.LPWStr)] string pszFile);

  }


  [
    ComImport(),
    Guid("00021401-0000-0000-C000-000000000046")
  ]
  public class ShellLink  // : IPersistFile, IShellLinkA, IShellLinkW 
  {
  }

}
