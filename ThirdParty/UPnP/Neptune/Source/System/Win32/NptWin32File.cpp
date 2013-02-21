/*****************************************************************
|
|      Neptune - File :: Win32 Implementation
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#include "NptLogging.h"
#include "NptFile.h"
#include "NptUtils.h"

#if defined(_XBOX)
#include <xtl.h>
#else
#include <windows.h>
#include <malloc.h>
#include <limits.h>
#endif
#include <assert.h>

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.win32.file")

/*----------------------------------------------------------------------
|   A2WHelper
+---------------------------------------------------------------------*/
static LPWSTR A2WHelper(LPWSTR lpw, LPCSTR lpa, int nChars, UINT acp)
{
    int ret;

    assert(lpa != NULL);
    assert(lpw != NULL);
    if (lpw == NULL || lpa == NULL) return NULL;

    lpw[0] = '\0';
    ret = MultiByteToWideChar(acp, 0, lpa, -1, lpw, nChars);
    if (ret == 0) {
        assert(0);
        return NULL;
    }        
    return lpw;
}

/*----------------------------------------------------------------------
|   W2AHelper
+---------------------------------------------------------------------*/
static LPSTR W2AHelper(LPSTR lpa, LPCWSTR lpw, int nChars, UINT acp)
{
    int ret;

    assert(lpw != NULL);
    assert(lpa != NULL);
    if (lpa == NULL || lpw == NULL) return NULL;

    lpa[0] = '\0';
    ret = WideCharToMultiByte(acp, 0, lpw, -1, lpa, nChars, NULL, NULL);
    if (ret == 0) {
        int error = GetLastError();
        assert(error);
        return NULL;
    }
    return lpa;
}

/*----------------------------------------------------------------------
|   macros
+---------------------------------------------------------------------*/
/* UNICODE support */
#if !defined(_XBOX)
#define NPT_WIN32_USE_CHAR_CONVERSION int _convert = 0; LPCWSTR _lpw = NULL; LPCSTR _lpa = NULL

#define NPT_WIN32_A2W(lpa) (\
    ((_lpa = lpa) == NULL) ? NULL : (\
    _convert = (int)(strlen(_lpa)+1),\
    (INT_MAX/2<_convert)? NULL :  \
    A2WHelper((LPWSTR) alloca(_convert*sizeof(WCHAR)), _lpa, _convert, CP_UTF8)))

/* +2 instead of +1 temporary fix for Chinese characters */
#define NPT_WIN32_W2A(lpw) (\
    ((_lpw = lpw) == NULL) ? NULL : (\
    (_convert = (lstrlenW(_lpw)+2), \
    (_convert>INT_MAX/2) ? NULL : \
    W2AHelper((LPSTR) alloca(_convert*sizeof(WCHAR)), _lpw, _convert*sizeof(WCHAR), CP_UTF8))))

#else
#define NPT_WIN32_USE_CHAR_CONVERSION
#define NPT_WIN32_W2A(_s)       (_s)
#define NPT_WIN32_A2W(_s)       (_s)
#define GetFileAttributesW      GetFileAttributes
#define FindFirstFileW          FindFirstFile
#define FindNextFileW           FindNextFile
#define FindCloseW              FindClose
#define CreateDirectoryW        CreateDirectoryA
#define RemoveDirectoryW        RemoveDirectoryA
#define DeleteFileW             DeleteFileA
#define MoveFileW               MoveFileA
#define WIN32_FIND_DATAW        WIN32_FIND_DATA
#endif

/*----------------------------------------------------------------------
|   MapError
+---------------------------------------------------------------------*/
static NPT_Result
MapError(DWORD err) {
    switch (err) {
      case ERROR_ALREADY_EXISTS:      return NPT_ERROR_FILE_ALREADY_EXISTS;
      case ERROR_PATH_NOT_FOUND:    
      case ERROR_FILE_NOT_FOUND:    
      case ERROR_INVALID_DRIVE:
      case ERROR_BAD_PATHNAME:
      case ERROR_BAD_NET_NAME:
      case ERROR_FILENAME_EXCED_RANGE:
      case ERROR_NO_MORE_FILES:
      case ERROR_BAD_NETPATH:         return NPT_ERROR_NO_SUCH_FILE;
      case ERROR_LOCK_VIOLATION:
      case ERROR_SEEK_ON_DEVICE:
      case ERROR_CURRENT_DIRECTORY:
      case ERROR_CANNOT_MAKE:
      case ERROR_FAIL_I24:
      case ERROR_NETWORK_ACCESS_DENIED:
      case ERROR_DRIVE_LOCKED:
      case ERROR_ACCESS_DENIED:       return NPT_ERROR_PERMISSION_DENIED;
      case ERROR_NOT_LOCKED:
      case ERROR_LOCK_FAILED:
      case ERROR_SHARING_VIOLATION:   return NPT_ERROR_FILE_BUSY;
      case ERROR_INVALID_FUNCTION:    return NPT_ERROR_INTERNAL;
      case ERROR_NOT_ENOUGH_QUOTA:    return NPT_ERROR_OUT_OF_MEMORY;
      case ERROR_ARENA_TRASHED:
      case ERROR_NOT_ENOUGH_MEMORY:
      case ERROR_INVALID_BLOCK:       return NPT_ERROR_OUT_OF_MEMORY;
      case ERROR_DISK_FULL:           return NPT_ERROR_FILE_NOT_ENOUGH_SPACE;
      case ERROR_TOO_MANY_OPEN_FILES: return NPT_ERROR_OUT_OF_RESOURCES;
      case ERROR_INVALID_HANDLE:      
      case ERROR_INVALID_ACCESS:
      case ERROR_INVALID_DATA:        return NPT_ERROR_INVALID_PARAMETERS;
      case ERROR_DIR_NOT_EMPTY:       return NPT_ERROR_DIRECTORY_NOT_EMPTY;
      case ERROR_NEGATIVE_SEEK:       return NPT_ERROR_OUT_OF_RANGE;
      default:                        return NPT_FAILURE;
    }
}

#if defined(_WIN32_WCE)
/*----------------------------------------------------------------------
|   NPT_stat_utf8
+---------------------------------------------------------------------*/
int
NPT_stat_utf8(const char* path, struct __stat64* info)
{
    return -1;
}

/*----------------------------------------------------------------------
|   NPT_getcwd_utf8
+---------------------------------------------------------------------*/
char*
NPT_getcwd_utf8(char* dir, int max_size)
{
    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_fopen_utf8
+---------------------------------------------------------------------*/
FILE*
NPT_fopen_utf8(const char* path, const char* mode)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    return _wfopen(NPT_WIN32_A2W(path), NPT_WIN32_A2W(mode));
}
#elif defined(_XBOX)
#include <sys/stat.h>
/*----------------------------------------------------------------------
|   NPT_stat_utf8
+---------------------------------------------------------------------*/
int
NPT_stat_utf8(const char* path, struct __stat64* info)
{
    return _stat64(path, info);
}

/*----------------------------------------------------------------------
|   NPT_getcwd_utf8
+---------------------------------------------------------------------*/
char*
NPT_getcwd_utf8(char* dir, unsigned int max_size)
{
    return NULL;
}

/*----------------------------------------------------------------------
|   NPT_fsopen_utf8
+---------------------------------------------------------------------*/
FILE*
NPT_fsopen_utf8(const char* path, const char* mode, int sh_flags)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    return _fsopen(path, mode, sh_flags);
}
#else
#include <sys/stat.h>
#include <direct.h>

/*----------------------------------------------------------------------
|   NPT_stat_utf8
+---------------------------------------------------------------------*/
int
NPT_stat_utf8(const char* path, struct __stat64* info)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    return _wstat64(NPT_WIN32_A2W(path), info);
}

/*----------------------------------------------------------------------
|   NPT_getcwd_utf8
+---------------------------------------------------------------------*/
char*
NPT_getcwd_utf8(char* dir, unsigned int max_size)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    WCHAR* wdir = (WCHAR*)alloca(2*(max_size+1));
    WCHAR* result = _wgetcwd(wdir, max_size);
    if (result == NULL) return NULL;
    char* converted = NPT_WIN32_W2A(result);
    NPT_CopyString(dir, converted);
    return dir;
}

/*----------------------------------------------------------------------
|   NPT_fsopen_utf8
+---------------------------------------------------------------------*/
FILE*
NPT_fsopen_utf8(const char* path, const char* mode, int sh_flags)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    return _wfsopen(NPT_WIN32_A2W(path), NPT_WIN32_A2W(mode + NPT_String(", ccs=UNICODE")), sh_flags);
}
#endif

/*----------------------------------------------------------------------
|   NPT_FilePath::Separator
+---------------------------------------------------------------------*/
const char* const NPT_FilePath::Separator = "\\";

/*----------------------------------------------------------------------
|   NPT_File::GetRoots
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetRoots(NPT_List<NPT_String>& roots)
{
    roots.Clear();
#if defined(_WIN32_WCE) || defined(_XBOX)
    return NPT_ERROR_NOT_IMPLEMENTED;
#else
    DWORD drives = GetLogicalDrives();
    for (unsigned int i=0; i<26; i++) {
        if (drives & (1<<i)) {
            char drive_name[4] = {'A'+i, ':', '\\', 0};
            roots.Add(drive_name);
        }
    }
    return NPT_SUCCESS;
#endif
}

#if defined(_WIN32_WCE)
/*----------------------------------------------------------------------
|   NPT_File::GetWorkingDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetWorkingDir(NPT_String& path)
{
    path.SetLength(0);
    return NPT_ERROR_NOT_IMPLEMENTED;
}

/*----------------------------------------------------------------------
|   NPT_File::GetInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetInfo(const char* path, NPT_FileInfo* info)
{
    return NPT_ERROR_NOT_IMPLEMENTED;
}
#endif

/*----------------------------------------------------------------------
|   NPT_File::CreateDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::CreateDir(const char* path)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    BOOL result = ::CreateDirectoryW(NPT_WIN32_A2W(path), NULL);
    if (result == 0) {
        return MapError(GetLastError());
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveFile
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveFile(const char* path)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    BOOL result = ::DeleteFileW(NPT_WIN32_A2W(path));
    if (result == 0) {
        return MapError(GetLastError());
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveDir(const char* path)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    BOOL result = RemoveDirectoryW(NPT_WIN32_A2W(path));
    if (result == 0) {
        return MapError(GetLastError());
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::Rename
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Rename(const char* from_path, const char* to_path)
{
    NPT_WIN32_USE_CHAR_CONVERSION;
    BOOL result = MoveFileW(NPT_WIN32_A2W(from_path), NPT_WIN32_A2W(to_path));
    if (result == 0) {
        return MapError(GetLastError());
    }
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File_ProcessFindData
+---------------------------------------------------------------------*/
static bool
NPT_File_ProcessFindData(WIN32_FIND_DATAW* find_data)
{
    NPT_WIN32_USE_CHAR_CONVERSION;

    // discard system specific files/shortcuts
    if (NPT_StringsEqual(NPT_WIN32_W2A(find_data->cFileName), ".") || 
        NPT_StringsEqual(NPT_WIN32_W2A(find_data->cFileName), "..")) {
        return false;
    }

    return true;
}

/*----------------------------------------------------------------------
|   NPT_File::ListDir
+---------------------------------------------------------------------*/
NPT_Result 
NPT_File::ListDir(const char*           path, 
                  NPT_List<NPT_String>& entries, 
                  NPT_Ordinal           start /* = 0 */, 
                  NPT_Cardinal          max   /* = 0 */)
{
    NPT_WIN32_USE_CHAR_CONVERSION;

    // default return value
    entries.Clear();

    // check the arguments
    if (path == NULL || path[0] == '\0') return NPT_ERROR_INVALID_PARAMETERS;

    // construct a path name with a \* wildcard at the end
    NPT_String path_pattern = path;
    if (path_pattern.EndsWith("\\") || path_pattern.EndsWith("/")) {
        path_pattern += "*";
    } else {
        path_pattern += "\\*";
    }

    // list the entries
    WIN32_FIND_DATAW find_data;
    HANDLE find_handle = FindFirstFileW(NPT_WIN32_A2W(path_pattern.GetChars()), &find_data);
    if (find_handle == INVALID_HANDLE_VALUE) return MapError(GetLastError());
    NPT_Cardinal count = 0;
    do {
        if (NPT_File_ProcessFindData(&find_data)) {
            // continue if we still have entries to skip
            if (start > 0) {
                --start;
                continue;
            }
            entries.Add(NPT_WIN32_W2A(find_data.cFileName));

            // stop when we have reached the maximum requested
            if (max && ++count == max) return NPT_SUCCESS;
        }
    } while (FindNextFileW(find_handle, &find_data));
    DWORD last_error = GetLastError();
    FindClose(find_handle);
    if (last_error != ERROR_NO_MORE_FILES) return MapError(last_error);

    return NPT_SUCCESS;
}
