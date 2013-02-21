/*****************************************************************
|
|      Neptune - File :: Posix Implementation
|
|      (c) 2001-2008 Gilles Boccon-Gibod
|      Author: Gilles Boccon-Gibod (bok@bok.net)
|
 ****************************************************************/

/*----------------------------------------------------------------------
|   includes
+---------------------------------------------------------------------*/
#define _LARGEFILE_SOURCE
#define _LARGEFILE_SOURCE64
#define _FILE_OFFSET_BITS 64

#include <sys/stat.h>

#include <errno.h>

#if defined(_WIN32)
#include <direct.h>
#include <stdlib.h>
#include <stdio.h>
#else
#include <unistd.h>
#include <dirent.h>
#endif

#include "NptConfig.h"
#include "NptLogging.h"
#include "NptFile.h"
#include "NptUtils.h"

/*----------------------------------------------------------------------
|   logging
+---------------------------------------------------------------------*/
//NPT_SET_LOCAL_LOGGER("neptune.posix.file")

/*----------------------------------------------------------------------
|   Win32 adaptation
+---------------------------------------------------------------------*/
#if defined(_WIN32) || defined(_XBOX)
extern int NPT_stat_utf8(const char* path, NPT_stat_struct* info);
extern char* NPT_getcwd_utf8(char* path, unsigned int path_size);
#define getcwd NPT_getcwd_utf8
#define S_ISDIR(_m) (((_m)&_S_IFMT) == _S_IFDIR) 
#define S_ISREG(_m) (((_m)&_S_IFMT) == _S_IFREG) 
#define S_IWUSR _S_IWRITE
#endif

/*----------------------------------------------------------------------
|   MapErrno
+---------------------------------------------------------------------*/
static NPT_Result
MapErrno(int err) {
    switch (err) {
      case EACCES:       return NPT_ERROR_PERMISSION_DENIED;
      case EPERM:        return NPT_ERROR_PERMISSION_DENIED;
      case ENOENT:       return NPT_ERROR_NO_SUCH_FILE;
      case ENAMETOOLONG: return NPT_ERROR_INVALID_PARAMETERS;
      case EBUSY:        return NPT_ERROR_FILE_BUSY;
      case EROFS:        return NPT_ERROR_FILE_NOT_WRITABLE;
      case ENOTDIR:      return NPT_ERROR_FILE_NOT_DIRECTORY;
      case EEXIST:       return NPT_ERROR_FILE_ALREADY_EXISTS;
      case ENOSPC:       return NPT_ERROR_FILE_NOT_ENOUGH_SPACE;
      case ENOTEMPTY:    return NPT_ERROR_DIRECTORY_NOT_EMPTY;
      default:           return NPT_ERROR_ERRNO(err);
    }
}

#if !defined(_WIN32)
/*----------------------------------------------------------------------
|   NPT_FilePath::Separator
+---------------------------------------------------------------------*/
const char* const NPT_FilePath::Separator = "/";

/*----------------------------------------------------------------------
|   NPT_File::GetRoots
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetRoots(NPT_List<NPT_String>& roots)
{
    roots.Clear();
    roots.Add("/");
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::CreateDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::CreateDir(const char* path)
{
    int result;
    
    result = mkdir(path, 0755);
    if (result != 0) {
        return MapErrno(errno);
    }
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveFile
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveFile(const char* path)
{
    int result = unlink(path);
    if (result != 0) return MapErrno(errno);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::RemoveDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::RemoveDir(const char* path)
{
    int result = rmdir(path);
    if (result != 0) return MapErrno(errno);
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::Rename
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::Rename(const char* from_path, const char* to_path)
{
    int result = rename(from_path, to_path);
    if (result != 0) return MapErrno(errno);

    return NPT_SUCCESS;
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
    // default return value
    entries.Clear();
    
    // check the arguments
    if (path == NULL) return NPT_ERROR_INVALID_PARAMETERS;
    
    // list the entries
    DIR *directory = opendir(path);
    if (directory == NULL) return NPT_ERROR_NO_SUCH_ITEM;
    
    NPT_Cardinal count = 0;
    for (;;) {
        struct dirent* entry_pointer = NULL;
#if defined(NPT_CONFIG_HAVE_READDIR_R)
        struct dirent entry;
        int result = readdir_r(directory, &entry, &entry_pointer);
        if (result != 0 || entry_pointer == NULL) break;
#else
        entry_pointer = readdir(directory);
        if (entry_pointer == NULL) break;
#endif
        // ignore odd names
        if (entry_pointer->d_name[0] == '\0') continue;

        // ignore . and ..
        if (entry_pointer->d_name[0] == '.' && 
            entry_pointer->d_name[1] == '\0') {
            continue;
        }
        if (entry_pointer->d_name[0] == '.' && 
            entry_pointer->d_name[1] == '.' &&
            entry_pointer->d_name[2] == '\0') {
            continue;
        }        
        
        // continue if we still have some items to skip
        if (start > 0) {
            --start;
            continue;
        }
        entries.Add(NPT_String(entry_pointer->d_name));

        // stop when we have reached the maximum requested
        if (max && ++count == max) break;
    }
    
    closedir(directory);
    
    return NPT_SUCCESS;
}
#endif

/*----------------------------------------------------------------------
|   NPT_File::GetWorkingDir
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetWorkingDir(NPT_String& path)
{
    char buffer[1024+1];
    char* dir = getcwd(buffer, 1024+1);
    if (dir == NULL) return MapErrno(errno);
    path = dir;
    
    return NPT_SUCCESS;
}

/*----------------------------------------------------------------------
|   NPT_File::GetInfo
+---------------------------------------------------------------------*/
NPT_Result
NPT_File::GetInfo(const char* path, NPT_FileInfo* info)
{
    // default value
    if (info) NPT_SetMemory(info, 0, sizeof(*info));
    
#if defined(_WIN32) || defined(_XBOX)
    // On Windows, stat will fail if a dir ends with a separator
    NPT_String _path = path;
    _path.TrimRight("\\/");
    // keep a separator at the end for drive names such as C:<backslash>
    if (NPT_StringLength(_path) ==  2 && _path[1] == ':') {
        _path += NPT_FilePath::Separator;
    }
#else
#define _path path
#endif

    // get the file info
    NPT_stat_struct stat_buffer;
    int result = NPT_stat(_path, &stat_buffer);
    if (result != 0) return MapErrno(errno);
    
    // setup the returned fields
    if (info) {
        info->m_Size = stat_buffer.st_size;
        if (S_ISREG(stat_buffer.st_mode)) {
            info->m_Type = NPT_FileInfo::FILE_TYPE_REGULAR;
        } else if (S_ISDIR(stat_buffer.st_mode)) {
            info->m_Type = NPT_FileInfo::FILE_TYPE_DIRECTORY;
        } else {
            info->m_Type = NPT_FileInfo::FILE_TYPE_OTHER;
        }
        info->m_AttributesMask &= NPT_FILE_ATTRIBUTE_READ_ONLY;
        if ((stat_buffer.st_mode & S_IWUSR) == 0) {
            info->m_Attributes &= NPT_FILE_ATTRIBUTE_READ_ONLY;
        }
#if defined(NPT_CONFIG_HAVE_STAT_ST_BIRTHTIME)
        info->m_CreationTime.SetSeconds(stat_buffer.st_birthtime);
#elif defined(NPT_CONFIG_STAT_ST_CTIME_IS_ST_BIRTHTIME)
        info->m_CreationTime.SetSeconds(stat_buffer.st_ctime);
#else
        info->m_CreationTime.SetSeconds(0);
#endif
        info->m_ModificationTime.SetSeconds(stat_buffer.st_mtime);
    }
    
    return NPT_SUCCESS;
}
